using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Managers;
using TTvActionHub.Twitch;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Websockets;
using Windows.Web.Syndication;
using TwitchLib.Client.Events;
using TTvActionHub.LuaTools.Stuff;
using TwitchLib.Communication.Events;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using Microsoft.UI.Xaml.Controls;

namespace TTvActionHub.Services
{
    public class TwitchService : IService, IUpdatableConfiguration
    {
        // --- IConfig impl ---
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public string ServiceName => nameof(TwitchService);
        public bool IsRunning { get; private set; } = false;

        // --- Connection checks

        public bool IsTwitchClientConnected { get => _twitchClient?.IsConnected ?? false; }
        public bool IsEventSubClientConnected { get; private set; } = false;

        // --- Configuration constants ---
        private const int ReconnectDelaySeconds = 5;
        private const int MaxConcurrentEvents = 5;
        private const int WorkerQueuePollDelayMs = 100;
        private const int EventActionShutdownWaitSeconds = 3;
        private const int WorkerTaskShutdownWaitSeconds = 3;

        // --- Configuration ---
        private readonly LuaConfigManager _configManager;
        private readonly IConfig _configuration;

        // --- Twitch related fields ---
        private TwitchClient? _twitchClient;
        private EventSubWebsocketClient? _eventSubClient;
        private TwitchApi? _twitchApi;

        // --- Events Handler ---
        public ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>? TwitchEvents { get; private set; }
        private ConcurrentQueue<(TwitchEvent Event, TwitchEventArgs Args)>? _eventsQueue;
        private SemaphoreSlim? _eventsSemaphore;
        private Task? _queueWorkerTask;

        // --- Thread safe objects --- 
        private readonly object _connectionLock = new();
        private volatile bool _stopRequested = false;
        private CancellationTokenSource? _serviceCts;
        private ConnectionCredentials? _credentials;

        public TwitchService(IConfig configuration, LuaConfigManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            TwitchEvents = _configManager.LoadTwitchEvents()
                ?? throw new InvalidOperationException($"Failed to load initial TwitchEvents configuration for {ServiceName}");
        }

        // --- Methods for LuaBridges ---

        public void SendMessage(string message)
        {
            _twitchClient?.SendMessage(_configuration.Login, message);
        }
        
        public void SendWhisper(string target, string message)
        {
            _twitchClient?.SendWhisper(target, message);
        }

        // --- Running and Stopping service --- 

        public void Run()
        {
            _stopRequested = false;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Starting service ...");
            lock (_connectionLock)
            {
                if (IsRunning && (IsTwitchClientConnected || IsEventSubClientConnected))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Service is already marked as running and at least one client is connected.");
                    bool needsTwitchClientReconnect = _twitchClient == null || !IsTwitchClientConnected;
                    bool needsEventSubClientReconnect = _eventSubClient == null || !IsEventSubClientConnected;

                    if (!needsTwitchClientReconnect && !needsEventSubClientReconnect)
                    {
                        OnStatusChanged(true, "Service already running and clients connected.");
                        return;
                    }

                    Logger.Log(LOGTYPE.INFO, ServiceName, "Service marked as running, but one or more clients need connection. Proceeding...");
                }

                if (_twitchClient != null && !IsTwitchClientConnected)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "TwitchClient instance exists but not connected. Cleaning up before restart attempt.");
                    CleanupTwitchClientResources();
                }
                
                if (_eventSubClient != null && !IsEventSubClientConnected)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "EventSubClient instance exists but not connected. Cleaning up before restart attempt.");
                    CleanupEventSubClientResources();
                }

                try
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing resources and clients...");

                    _serviceCts ??= new CancellationTokenSource();
                    _eventsQueue ??= new ConcurrentQueue<(TwitchEvent, TwitchEventArgs)>();
                    _eventsSemaphore ??= new SemaphoreSlim(MaxConcurrentEvents, MaxConcurrentEvents);
                    
                    if (_twitchClient == null)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing Twitch Chat client...");
                        _twitchClient = new TwitchClient();
                        _credentials = new ConnectionCredentials(_configuration.Login, _configuration.Token);
                        SubscribeToTwitchClientEvents();
                        _twitchClient.Initialize(_credentials, _configuration.Login);
                    }
                    if (!IsTwitchClientConnected) 
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Connecting Twitch Chat client...");
                        _twitchClient.Connect();
                    }

                    if (_eventSubClient == null)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing EventSub client...");
                        _eventSubClient = new EventSubWebsocketClient();
                        SubscribeToEventSubClientEvents();
                    }
                    if (!IsEventSubClientConnected) 
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Connecting EventSub client...");
                        _ = _eventSubClient.ConnectAsync();
                    }

                    if (_queueWorkerTask == null || _queueWorkerTask.IsCompleted)
                    {
                        if (_serviceCts.IsCancellationRequested) 
                        {
                            _serviceCts.Dispose();
                            _serviceCts = new CancellationTokenSource();
                        }
                        _queueWorkerTask = Task.Run(() => ProcessEventsQueueAsync(_serviceCts.Token), _serviceCts.Token);
                    }

                    IsRunning = true;
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start or connect one or more clients.", ex);
                    OnStatusChanged(false, $"Startup failed: {ex.Message}");
                    IsRunning = false;
                    StopInternal(true);
                    CleanupFullResources();
                }
            }
        }
        
        public void Stop()
        {
            if (_stopRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Stop already requested.");
                return;
            }
            _stopRequested = true;
            IsRunning = false;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Stopping service requested...");
            StopInternal(true);
            
            lock (_connectionLock)
            {
                DisconnectTwitchClient();
                DisconnectEventSubClient(); // Убедимся, что он также отключается
                CleanupTwitchClientResources();
                CleanupEventSubClientResources();
            }

            CleanupNonClientResources();

            Logger.Log(LOGTYPE.INFO, ServiceName, "Service stopped.");
            OnStatusChanged(false, "Service stopped by request.");
        }

        private void StopInternal(bool waitWorker)
        {
            if (_serviceCts != null && !_serviceCts.IsCancellationRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Requesting cancellation for event queue worker task...");
                try { _serviceCts.Cancel(); }
                catch (ObjectDisposedException) { /* Expected */ }
            }
            
            if (waitWorker && _queueWorkerTask != null && !_queueWorkerTask.IsCompleted)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Waiting for event queue worker task to stop...");
                try
                {
                    if (!_queueWorkerTask.Wait(TimeSpan.FromSeconds(WorkerTaskShutdownWaitSeconds)))
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Event queue worker task did not complete within the timeout period.");
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Event queue worker task stopped.");
                    }
                }
                catch (OperationCanceledException) { Logger.Log(LOGTYPE.INFO, ServiceName, "Event queue worker task wait was canceled (expected)."); }
                catch (AggregateException ae) { ae.Handle(ex => { Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception occurred in worker task during shutdown.", ex); return true; }); }
                catch (ObjectDisposedException) { Logger.Log(LOGTYPE.WARNING, ServiceName, "Worker task was already disposed during wait."); }
            }
            _queueWorkerTask = null;
        }

        private void SubscribeToTwitchClientEvents()
        {
            if (_twitchClient == null) return;
            _twitchClient.OnChatCommandReceived += TwitchClient_OnChatCommandReceived;
            _twitchClient.OnConnectionError += TwitchClient_OnConnectionError;
            _twitchClient.OnDisconnected += TwitchClient_OnDisconnected;
            _twitchClient.OnConnected += TwitchClient_OnConnected;
            _twitchClient.OnError += TwitchClient_OnError;
            if (_configuration.LogState) // Предполагаем, что IConfig имеет LogState
                _twitchClient.OnLog += TwitchClient_OnLog;
        }

        private void UnsubscribeFromTwitchClientEvents()
        {
            if (_twitchClient == null) return;
            _twitchClient.OnChatCommandReceived -= TwitchClient_OnChatCommandReceived;
            _twitchClient.OnConnectionError -= TwitchClient_OnConnectionError;
            _twitchClient.OnDisconnected -= TwitchClient_OnDisconnected;
            _twitchClient.OnConnected -= TwitchClient_OnConnected;
            _twitchClient.OnError -= TwitchClient_OnError;
            if (_configuration.LogState)
                _twitchClient.OnLog -= TwitchClient_OnLog;
        }

        private void SubscribeToEventSubClientEvents()
        {
            if (_eventSubClient == null) return;
            _eventSubClient.WebsocketConnected += EventSubClient_WebsocketConnectedHandler;
            _eventSubClient.WebsocketDisconnected += EventSubClient_WebsocketDisconnectedHandler;
            _eventSubClient.ErrorOccurred += EventSubClient_ErrorOccurredHandler;
            _eventSubClient.ChannelPointsCustomRewardRedemptionAdd += EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler;
            // Добавьте другие необходимые подписки EventSub здесь
        }

        private void UnsubscribeFromEventSubClientEvents()
        {
            if (_eventSubClient == null) return;
            _eventSubClient.WebsocketConnected -= EventSubClient_WebsocketConnectedHandler;
            _eventSubClient.WebsocketDisconnected -= EventSubClient_WebsocketDisconnectedHandler;
            _eventSubClient.ErrorOccurred -= EventSubClient_ErrorOccurredHandler;
            _eventSubClient.ChannelPointsCustomRewardRedemptionAdd -= EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler;
        }

        private void DisconnectTwitchClient()
        {
            if (IsTwitchClientConnected)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Disconnecting Twitch Chat client...");
                try
                {
                    _twitchClient?.Disconnect();
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during Twitch Chat client disconnect request.", ex);
                }
            }
        }

        private void DisconnectEventSubClient()
        {
            if (IsEventSubClientConnected && _eventSubClient != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Requesting EventSub WebSocket disconnect...");
                try
                {
                    _ = _eventSubClient.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during EventSub client disconnect request.", ex);
                }
                IsEventSubClientConnected = false;
            }
        }

        private void EnqueueTwitchEvent(TwitchEvent twitchEvent, TwitchEventArgs eventArgs)
        {
            if (_eventsQueue == null || _serviceCts?.IsCancellationRequested == true)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"Cannot enqueue {twitchEvent.Name}: Queue is null or service is stopping.");
                return;
            }

            Logger.Log(LOGTYPE.INFO, ServiceName, $"Queueing {twitchEvent.Name} from {eventArgs.Sender}.");
            _eventsQueue.Enqueue((twitchEvent, eventArgs));
        }

        private async Task ProcessEventsQueueAsync(CancellationToken cancellationToken)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Event processing worker started.");
            var runningEventTasks = new List<Task>();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    runningEventTasks.RemoveAll(t => t.IsCompleted); 

                    if (_eventsQueue != null && _eventsQueue.TryDequeue(out var eventDataToProcess))
                    {
                        if (_eventsSemaphore == null) 
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, "Events semaphore is null, cannot process event. This is a critical error.");
                            await Task.Delay(WorkerQueuePollDelayMs * 10, cancellationToken); 
                            _eventsQueue.Enqueue(eventDataToProcess);
                            continue;
                        }

                        if (_eventsSemaphore.CurrentCount == 0)
                        {
                            await Task.Delay(WorkerQueuePollDelayMs / 2, cancellationToken);
                            _eventsQueue.Enqueue(eventDataToProcess);
                            continue;
                        }

                        try
                        {
                            await _eventsSemaphore.WaitAsync(cancellationToken);

                            Task eventTask = Task.Run(() =>
                            {
                                string eventIdentifier = $"event '{eventDataToProcess.Event.Name}' ({eventDataToProcess.Event.Kind}) for {eventDataToProcess.Args.Sender}";
                                try
                                {
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Executing {eventIdentifier}...");
                                    eventDataToProcess.Event.Execute(eventDataToProcess.Args);
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Finished {eventIdentifier}.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error executing {eventIdentifier}", ex);
                                }
                                finally
                                {
                                    _eventsSemaphore.Release();
                                }
                            }, cancellationToken); 

                            runningEventTasks.Add(eventTask);
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Operation canceled while waiting for semaphore or starting event task.");
                            _eventsQueue.Enqueue(eventDataToProcess); 
                            break; 
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error dispatching event '{eventDataToProcess.Event.Name}':", ex);
                            _eventsSemaphore?.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(WorkerQueuePollDelayMs, cancellationToken); 
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Event processing loop was canceled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unhandled error in event processing loop.", ex);
            }
            finally
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Event worker loop finished. Waiting for {runningEventTasks.Count(t => !t.IsCompleted)} running event action(s) to complete...");
                try
                {
                    await Task.WhenAll([.. runningEventTasks]).WaitAsync(TimeSpan.FromSeconds(EventActionShutdownWaitSeconds), CancellationToken.None); 
                    Logger.Log(LOGTYPE.INFO, ServiceName, "All tracked event actions finished or timed out.");
                }
                catch (TimeoutException)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Timeout waiting for running event actions to complete upon shutdown.");
                }
                catch (Exception ex) 
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"Exception while waiting for running event actions: {ex.Message}");
                }
                Logger.Log(LOGTYPE.INFO, ServiceName, "Event processing worker stopped.");
            }
        }

        private void CleanupTwitchClientResources()
        {
            if (_twitchClient != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up Twitch Chat client resources...");
                UnsubscribeFromTwitchClientEvents();
                if (IsTwitchClientConnected)
                { 
                    _twitchClient.Disconnect(); 
                }
                _twitchClient = null;
                _credentials = null;
                Logger.Log(LOGTYPE.INFO, ServiceName, "Twitch Chat client resources cleaned up.");
            }
        }

        private void CleanupEventSubClientResources()
        {
            if (_eventSubClient != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up EventSub client resources...");
                UnsubscribeFromEventSubClientEvents();
                if (IsEventSubClientConnected)
                {
                    _ = _eventSubClient.DisconnectAsync(); 
                }
                _eventSubClient = null;
                IsEventSubClientConnected = false; 
                Logger.Log(LOGTYPE.INFO, ServiceName, "EventSub client resources cleaned up.");
            }
        }

        private void CleanupNonClientResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up non-client resources (CTS, Semaphore, Queue)...");

            try { _serviceCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _serviceCts?.Dispose(); } catch (ObjectDisposedException) { }
            _serviceCts = null;

            try { _eventsSemaphore?.Dispose(); } catch (ObjectDisposedException) { }
            _eventsSemaphore = null;

            _eventsQueue = null;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Non-client resources cleaned up.");
        }

        private void CleanupFullResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Performing full resource cleanup...");
            StopInternal(false); // Останавливаем worker без ожидания, так как все равно все чистим

            lock (_connectionLock) // Защищаем доступ к клиентам во время их очистки
            {
                DisconnectTwitchClient();
                DisconnectEventSubClient();
                CleanupTwitchClientResources();
                CleanupEventSubClientResources();
            }
            CleanupNonClientResources();
            Logger.Log(LOGTYPE.INFO, ServiceName, "Full resource cleanup finished.");
        }

        private void UpdateOverallStatus(string? specificMessage = null)
        {
            string message;
            bool currentServiceRunningStatus = IsRunning; // Текущее намерение сервиса работать

            if (currentServiceRunningStatus) // Если сервис должен работать
            {
                if (IsTwitchClientConnected && IsEventSubClientConnected)
                {
                    message = "All clients connected.";
                }
                else if (IsTwitchClientConnected)
                {
                    message = "Twitch Chat connected, EventSub disconnected.";
                }
                else if (IsEventSubClientConnected)
                {
                    message = "EventSub connected, Twitch Chat disconnected.";
                }
                else
                {
                    message = "All clients disconnected.";
                }
                if (!string.IsNullOrEmpty(specificMessage))
                {
                    message = $"{specificMessage} ({message})";
                }
            }
            else // Если сервис остановлен или останавливается
            {
                message = "Service not running.";
                if (!string.IsNullOrEmpty(specificMessage))
                {
                    message = $"{specificMessage}"; // Если есть specificMessage при остановке, он важнее
                }
                else if (_stopRequested)
                {
                    message = "Service stopping...";
                }
            }

            OnStatusChanged(currentServiceRunningStatus && (IsTwitchClientConnected || IsEventSubClientConnected), message);
            // Статус "running" для IService, если IsRunning=true И хотя бы один клиент работает.
            // Или можно просто передавать IsRunning, а сообщение будет содержать детали.
            // Выберу IsRunning для статуса, а сообщение для деталей.
            // OnStatusChanged(IsRunning, message);
        }

        // --- Status Changed event backend --- 
        private void OnStatusChanged(bool isRunning, string? message = null)
        {
            try
            {
                StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error invoking StatusChanged event handler.", ex);
            }
        }

        public bool UpdateConfiguration()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to update configuration...");
            try
            {
                if (_configManager.LoadTwitchEvents() is ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent> events)
                {
                    TwitchEvents = events;
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Configuration updated successfully.");
                    return true;
                }
                else
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Failed to update configuration: LoadTwitchEvents returned null.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to update configuration due to an error.", ex);
                return false;
            }
        }
        
        private async Task<bool> RegisterEventSubTopicsAsync()
        {
            if (_eventSubClient == null || string.IsNullOrEmpty(_eventSubClient.SessionId))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot register EventSub topics: Client is not connected or Session ID is missing.");
                return false;
            }
            if (string.IsNullOrEmpty(_configuration.ID)) // ID канала (Broadcaster User ID)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot register EventSub topics: Broadcaster User ID is missing in configuration.");
                return false;
            }
            if (_configuration.TwitchApi?.InnerAPI?.Helix?.EventSub == null)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Twitch API client for EventSub subscriptions is not configured or available.");
                return false;
            }

            bool allSuccess = true;
            
            var condition = new Dictionary<string, string> { { "broadcaster_user_id", _configuration.ID } };

            var rewardResult = await SubscribeToEventAsync("channel.channel_points_custom_reward_redemption.add", "1", condition); // channel points reward
            if (!rewardResult) allSuccess = false;

            return allSuccess;
        }

        private async Task<bool> SubscribeToEventAsync(string type, string version, Dictionary<string, string> condition)
        {
            if (_eventSubClient == null || string.IsNullOrEmpty(_eventSubClient.SessionId) || _configuration.TwitchApi?.InnerAPI?.Helix?.EventSub == null)
            {
                return false;
            }
            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to subscribe to [{type}:{version}]");
                var response = await _configuration.TwitchApi.InnerAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: type, version: version, condition: condition,
                    method: EventSubTransportMethod.Websocket, websocketSessionId: _eventSubClient.SessionId
                );
                if (response?.Subscriptions == null || response.Subscriptions.Length == 0)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"EventSub subscription request for [{type}:{version}] failed or returned empty data. Check scopes and broadcaster ID. Cost: {response?.TotalCost}, MaxCost: {response?.MaxTotalCost}");
                    return false;
                }
                bool subscriptionSuccessful = true;
                foreach (var sub in response.Subscriptions)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"EventSub subscription to [{sub.Type} v{sub.Version}] Status: {sub.Status}. Cost: {sub.Cost}. ID: {sub.Id}");
                    if (sub.Status != "enabled" && sub.Status != "webhook_callback_verification_pending") // "enabled" for websocket
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, $"EventSub subscription for [{sub.Type}] has status: {sub.Status}. Expected 'enabled'.");
                        
                        if (sub.Status.Contains("fail") || sub.Status.Contains("revoked") || sub.Status.Contains("error"))
                        {
                            subscriptionSuccessful = false;
                        }
                    }
                }
                if (!subscriptionSuccessful)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"One or more EventSub subscriptions for did not report 'enabled' status.");
                }
                return subscriptionSuccessful;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Failed to subscribe to EventSub topic [{type}:{version}] due to an API error.", ex);
                return false;
            }
        }

        // --- TwitchLib Events Handler ---

        private void TwitchClient_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            var chatCommand = args.Command;
            var cmdName = chatCommand.CommandText;
            
            if (TwitchEvents == null || !TwitchEvents.TryGetValue((cmdName, TwitchTools.TwitchEventKind.Command), out var twitchEvent) || twitchEvent == null)
            {
                //Logger.Log(LOGTYPE.DEBUG, ServiceName, $"Command '{cmdName}' not found or not configured.");
                return;
            }

            if (!twitchEvent.Executable)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Command '{cmdName}' from {chatCommand.ChatMessage.Username} cannot be executed right now (e.g., on cooldown).");
                return;
            }

            var senderUsername = chatCommand.ChatMessage.Username;
            var cmdArgStr = chatCommand.ArgumentsAsString.Replace("\U000e0000", "").Trim();
            string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');

            var userLevel = TwitchTools.ParceFromTwitchLib(
                chatCommand.ChatMessage.UserType,
                chatCommand.ChatMessage.IsSubscriber,
                chatCommand.ChatMessage.IsVip);

            var eventArgs = new TwitchEventArgs
            {
                Sender = senderUsername,
                Args = cmdArgs,
                Permission = userLevel
            };

            EnqueueTwitchEvent(twitchEvent, eventArgs);
        }

        private void TwitchClient_OnConnected(object? sender, OnConnectedArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Twitch Chat client connected to '{_configuration.Login}'.");
            UpdateOverallStatus("Twitch Chat Connected");
        }

        private void TwitchClient_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            Logger.Log(LOGTYPE.WARNING, ServiceName, "Twitch Chat client disconnected.");
            HandleClientDisconnection("Twitch Chat", "Disconnected");
        }

        private void TwitchClient_OnConnectionError(object? sender, OnConnectionErrorArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, $"Twitch Chat client connection error: {e.Error.Message}");
            HandleClientDisconnection("Twitch Chat", $"Connection Error: {e.Error.Message}");
        }

        private void TwitchClient_OnError(object? sender, OnErrorEventArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, "TwitchLib (Chat Client) internal error.", e.Exception);
        }

        private void TwitchClient_OnLog(object? sender, OnLogArgs e)
        {
            Logger.Log(LOGTYPE.INFO, $"{ServiceName} [TwitchClient Log]", e.Data);
        }


        private async Task EventSubClient_WebsocketConnectedHandler(object? sender, WebsocketConnectedArgs args)
        {
            IsEventSubClientConnected = true;
            Logger.Log(LOGTYPE.INFO, ServiceName, $"EventSub WebSocket connected. IsRequestedReconnect: {args.IsRequestedReconnect}");
            
            if (!args.IsRequestedReconnect && _eventSubClient != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to subscribe to EventSub topics...");
                try
                {
                    bool success = await RegisterEventSubTopicsAsync();
                    if (success)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Successfully subscribed to EventSub topics.");
                        UpdateOverallStatus("EventSub Connected and Subscribed");
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to subscribe to one or more EventSub topics.");
                        UpdateOverallStatus("EventSub Connected but Subscription Failed");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during EventSub topic subscription.", ex);
                    UpdateOverallStatus($"EventSub Connected but Subscription Error: {ex.Message}");
                }
            }
            else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "EventSub reconnected. Assuming subscriptions persist or will be re-established by TwitchLib if needed.");
                UpdateOverallStatus("EventSub Reconnected");
            }
        }

        private Task EventSubClient_WebsocketDisconnectedHandler(object? sender, EventArgs args)
        {
            IsEventSubClientConnected = false;
            Logger.Log(LOGTYPE.WARNING, ServiceName, "EventSub WebSocket disconnected.");
            HandleClientDisconnection("EventSub", "WebSocket Disconnected");
            return Task.CompletedTask;
        }

        private Task EventSubClient_ErrorOccurredHandler(object? sender, ErrorOccuredArgs args)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, $"EventSub client error: {args.Message}", args.Exception);
            return Task.CompletedTask;
        }

        private Task EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler(object? sender, ChannelPointsCustomRewardRedemptionArgs args)
        {
            var redemptionEvent = args.Notification.Payload.Event;
            var rewardTitle = redemptionEvent.Reward.Title;

            if (TwitchEvents == null || !TwitchEvents.TryGetValue((rewardTitle, TwitchTools.TwitchEventKind.TwitchReward), out var twitchEvent) || twitchEvent == null)
            {
                return Task.CompletedTask;
            }

            var senderUsername = redemptionEvent.UserName; 
            var rewardArgsStr = redemptionEvent.UserInput?.Trim() ?? string.Empty;
            string[]? rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');

            var eventArgs = new TwitchEventArgs
            {
                Sender = senderUsername,
                Args = rewardArgs,
                Permission = TwitchTools.PermissionLevel.VIEWIER 
            };

            EnqueueTwitchEvent(twitchEvent, eventArgs);
            return Task.CompletedTask;
        }

        private void HandleClientDisconnection(string clientName, string reason)
        {
            if (clientName == "EventSub")
            {
                IsEventSubClientConnected = false; 
            }

            UpdateOverallStatus($"{clientName} Disconnected: {reason}");

            if (!_stopRequested)
            {
                AttemptReconnect(clientName);
            }
            else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Reconnect for {clientName} skipped, service stop was requested.");
            }
        }

        private void AttemptReconnect(string? clientToReconnect = null)
        {
            if (_stopRequested) return;
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to reconnect {(clientToReconnect ?? "service")} in {ReconnectDelaySeconds} seconds...");

            Task.Run(async () =>
            {
                CancellationToken token = _serviceCts?.Token ?? CancellationToken.None;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds), token);

                    if (_stopRequested || token.IsCancellationRequested)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect cancelled (stop requested or token canceled).");
                        return;
                    }

                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Reconnecting {(clientToReconnect ?? "service")}...");
                    lock (_connectionLock) // Блокировка для изменения состояния клиентов
                    {
                        if (clientToReconnect == "Twitch Chat" && !IsTwitchClientConnected)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to reconnect Twitch Chat client specifically.");
                            CleanupTwitchClientResources();
                            _twitchClient = new();
                            _credentials = new(_configuration.Login, _configuration.Token);
                            _twitchClient.Initialize(_credentials, _configuration.Login);
                            SubscribeToTwitchClientEvents();
                            _twitchClient.Connect();

                        }
                        else if (clientToReconnect == "EventSub" && !IsEventSubClientConnected)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to reconnect EventSub client specifically.");
                            CleanupEventSubClientResources();
                            _eventSubClient = new();
                            SubscribeToEventSubClientEvents();
                            _ = _eventSubClient.ConnectAsync();

                        }
                        else if (clientToReconnect == null) // Общий запрос на реконнект
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting full service Run for reconnect.");
                            Run(); // Попытка запустить весь сервис (Run проверит, что нужно подключить)
                        }
                        else
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, $"Reconnect for {clientToReconnect} skipped, client is already connected or not specified for specific reconnect.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect delay or operation was canceled.");
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during reconnect attempt execution.", ex);
                    OnStatusChanged(false, $"Reconnect failed for {(clientToReconnect ?? "service")}: {ex.Message}");
                }
            });
        }
    
    
    }
}
