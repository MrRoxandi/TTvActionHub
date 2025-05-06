using System.Collections.Concurrent;
using TTvActionHub.Items; 
using TTvActionHub.Logs; 
using TTvActionHub.Managers; 
using TwitchLib.Api.Core.Enums; 
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets; 
using TwitchLib.EventSub.Websockets.Core.EventArgs; 
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace TTvActionHub.Services
{
    public class EventSubService : IService, IUpdatableConfiguration
    {
        private const int ReconnectDelaySeconds = 5;
        private const int WorkerQueuePollDelayMs = 100;
        private const int MaxConcurrentRewards = 5; 
        private const int RewardShutdownWaitSeconds = 3; 
        private const int WorkerTaskShutdownWaitSeconds = 3; 

        public EventSubWebsocketClient? Client => _client; 

        public ConcurrentDictionary<string, TwitchReward>? Rewards { get; private set; }
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;

        public bool IsRunning => _client != null;

        public string ServiceName => "EventSubService";

        private readonly LuaConfigManager _configManager;
        private readonly IConfig _configuration;
        private readonly object _connectionLock = new(); 

        private volatile bool _stopRequested = false;
        private volatile bool _isConnected = false;
        private EventSubWebsocketClient? _client;

        private CancellationTokenSource? _serviceCts;
        
        private ConcurrentQueue<(TwitchReward reward, string rewardTitle, string sender, string[]? args)>? _rewardsQueue;
        private Task? _workerTask; 
        private SemaphoreSlim? _rewardSemaphore; 

        public EventSubService(IConfig config, LuaConfigManager manager)
        {
            _configManager = manager ?? throw new ArgumentNullException(nameof(manager));
            _configuration = config ?? throw new ArgumentNullException(nameof(config));

            Rewards = _configManager.LoadRewards()
                ?? throw new InvalidOperationException($"Failed to load initial reward configuration for {ServiceName}");
        }

        public void Run()
        {
            _stopRequested = false;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Starting service...");

            lock (_connectionLock)
            {
                if (IsRunning) 
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Service instance already exists (may or may not be connected).");
                    if (_isConnected)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Service is already connected.");
                        OnStatusChanged(true); 
                        return;
                    }
                    
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Service instance exists but not connected. Attempting to connect/re-subscribe...");
                }
                else 
                {
                    CleanupClientResources(); 
                }


                try
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing EventSub client resources...");

                    _serviceCts ??= new CancellationTokenSource();
                    _rewardsQueue ??= new ConcurrentQueue<(TwitchReward reward, string rewardTitle, string sender, string[]? args)>();
                    _rewardSemaphore ??= new SemaphoreSlim(MaxConcurrentRewards, MaxConcurrentRewards); // Используем MaxConcurrentCommands

                    if (_client == null)
                    {
                        _client = new EventSubWebsocketClient();
                        SubscribeToClientEvents(); 
                    }

                    if (_workerTask == null || _workerTask.IsCompleted)
                    {
                        if (_serviceCts.IsCancellationRequested)
                        {
                            _serviceCts.Dispose();
                            _serviceCts = new CancellationTokenSource();
                        }
                        _workerTask = Task.Run(() => ProcessRewardQueueAsync(_serviceCts.Token), _serviceCts.Token);
                    }

                    Logger.Log(LOGTYPE.INFO, ServiceName, "Connecting to EventSub WebSocket...");
                    _ = _client.ConnectAsync(); 

                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start or connect.", ex);
                    OnStatusChanged(false, $"Startup failed: {ex.Message}");
                    StopInternal();
                    CleanupResources(); 
                }
            }
        }

        public void Stop()
        {
            if (_stopRequested) return;

            _stopRequested = true;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Stopping service requested...");

            StopInternal(); 

            lock (_connectionLock)
            {
                DisconnectClient(); 
                CleanupClientResources(); 
            }
            CleanupNonClientResources(); 

            Logger.Log(LOGTYPE.INFO, ServiceName, "Service stopped.");
            OnStatusChanged(false, "Service stopped by request.");
        }

        public bool UpdateConfiguration()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to update reward configuration...");
            try
            {
                if (_configManager.LoadRewards() is ConcurrentDictionary<string, TwitchReward> rwds)
                {
                    Rewards = rwds; 
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Reward configuration updated successfully.");
                    return true;
                }
                else
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Failed to update reward configuration: LoadRewards returned null.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to update reward configuration due to an error.", ex);
                return false;
            }
        }

        private void SubscribeToClientEvents()
        {
            if (_client == null) return;
            _client.WebsocketConnected += WebsocketConnectedHandler;
            _client.WebsocketDisconnected += WebsocketDisconnectedHandler;
            _client.ErrorOccurred += ErrorOccurredHandler;
            _client.ChannelPointsCustomRewardRedemptionAdd += ChannelPointsCustomRewardRedemptionAddHandler;
        }

        private void UnsubscribeFromClientEvents()
        {
            if (_client == null) return;
            _client.WebsocketConnected -= WebsocketConnectedHandler;
            _client.WebsocketDisconnected -= WebsocketDisconnectedHandler;
            _client.ErrorOccurred -= ErrorOccurredHandler;
            _client.ChannelPointsCustomRewardRedemptionAdd -= ChannelPointsCustomRewardRedemptionAddHandler;
        }

        private async Task WebsocketConnectedHandler(object? sender, WebsocketConnectedArgs args)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"WebSocket connected. Reconnect requested: {args.IsRequestedReconnect}");

            if (!args.IsRequestedReconnect && _client != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Subscribing to EventSub topics...");
                try
                {
                    bool success = await RegisterEventsAsync(); 
                    if (success)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Successfully subscribed to topics.");
                        OnStatusChanged(true, "Connected and Subscribed");
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to subscribe to one or more topics.");
                        OnStatusChanged(true, "Connected but Subscription Failed");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during event subscription.", ex);
                    OnStatusChanged(true, $"Connected but Subscription Error: {ex.Message}");
                }
            }
            else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnected. Assuming subscriptions persist.");
                OnStatusChanged(true, "Reconnected");
            }
            _isConnected = true;
        }

        private Task WebsocketDisconnectedHandler(object? sender, EventArgs args)
        {
            Logger.Log(LOGTYPE.WARNING, ServiceName, "WebSocket disconnected.");
            _isConnected = false;
            Task.Run(() => HandleDisconnection("WebSocket Disconnected"));
            return Task.CompletedTask;
        }

        private Task ErrorOccurredHandler(object? sender, ErrorOccuredArgs args)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, $"An error occurred: {args.Message}", args.Exception);
            //OnStatusChanged(_client?.IsConnected ?? false, $"Error: {args.Message}");
            return Task.CompletedTask;
        }

        private Task ChannelPointsCustomRewardRedemptionAddHandler(object? sender, ChannelPointsCustomRewardRedemptionArgs args)
        {
            EnqueueReward(args.Notification.Payload.Event);
            return Task.CompletedTask;
        }

        private void EnqueueReward(ChannelPointsCustomRewardRedemption redemptionEvent)
        {
            if (_rewardsQueue == null || Rewards == null || _serviceCts?.IsCancellationRequested == true)
            {
                return; 
            }

            var rewardTitle = redemptionEvent.Reward.Title;

            if (!Rewards.TryGetValue(rewardTitle, out var reward) || reward == null)
            {
                return; 
            }

            var sender = redemptionEvent.UserName;
            var rewardArgsStr = redemptionEvent.UserInput?.Trim() ?? string.Empty;
            string[]? rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');

            Logger.Log(LOGTYPE.INFO, ServiceName, $"Queueing reward: '{rewardTitle}' from {sender} with input: '{rewardArgsStr}'");

            _rewardsQueue.Enqueue((reward, rewardTitle, sender, rewardArgs));
        }

        private async Task ProcessRewardQueueAsync(CancellationToken cancellationToken)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Reward processing worker started.");
            var runningTasks = new List<Task>();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    runningTasks.RemoveAll(t => t.IsCompleted);

                    if (_rewardsQueue!.TryDequeue(out var runData))
                    {
                        if (_rewardSemaphore!.CurrentCount == 0)
                        {
                            await Task.Delay(WorkerQueuePollDelayMs, cancellationToken);
                            _rewardsQueue.Enqueue(runData); 
                            continue;
                        }

                        try
                        {
                            await _rewardSemaphore.WaitAsync(cancellationToken);
                            
                            Task rewardTask = Task.Run(() =>
                            {
                                string rewardIdentifier = $"'{runData.rewardTitle}' from {runData.sender}";
                                try
                                {
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Executing reward action {rewardIdentifier}...");
                                    runData.reward.Execute(runData.sender, runData.args);
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Finished reward action {rewardIdentifier}.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error executing reward action {rewardIdentifier}", ex);
                                }
                                finally
                                {
                                    _rewardSemaphore.Release();
                                }
                            }, cancellationToken); 

                            runningTasks.Add(rewardTask); 
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Operation canceled while waiting for semaphore or starting reward task.");
                            break; 
                        }
                        catch (Exception ex) 
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error dispatching reward '{runData.rewardTitle}': {ex.Message}", ex);
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
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reward processing loop was canceled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unhandled error in reward processing loop", ex);
            }
            finally
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Worker loop finished. Waiting for {runningTasks.Count} running reward action(s) to complete...");
                try
                {
                    await Task.WhenAll(runningTasks).WaitAsync(TimeSpan.FromSeconds(RewardShutdownWaitSeconds)); // Используем WaitAsync
                    Logger.Log(LOGTYPE.INFO, ServiceName, "All tracked reward actions finished or timed out.");
                }
                catch (TimeoutException)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Timeout waiting for running reward actions to complete upon shutdown.");
                }
                catch (Exception ex) 
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"Exception while waiting for running rewards: {ex.Message}");
                }
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reward processing worker stopped.");
            }
        }

        
        private async Task<bool> RegisterEventsAsync()
        {
            if (_client == null || string.IsNullOrEmpty(_client.SessionId))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot register events: Client is not connected or Session ID is missing.");
                return false;
            }
            if (string.IsNullOrEmpty(_configuration.ID))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot register events: Broadcaster User ID is missing in configuration.");
                return false;
            }
            if (_configuration.TwitchApi?.InnerAPI?.Helix?.EventSub == null)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Twitch API client for EventSub subscriptions is not configured or available.");
                return false;
            }

            bool allSuccess = true;

            var rewardResult = await SubscribeToEventAsync(
                "channel.channel_points_custom_reward_redemption.add", "1",
                new Dictionary<string, string> { { "broadcaster_user_id", _configuration.ID } }
            );
            if (rewardResult == null || rewardResult.Total == 0 || rewardResult.Subscriptions.Any(d => d.Status != "enabled"))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to subscribe to channel points redemptions.");
                allSuccess = false;
            }
            else LogSubscriptionResult(rewardResult, "channel points redemptions");

            return allSuccess;
        }

        private async Task<CreateEventSubSubscriptionResponse?> SubscribeToEventAsync(string type, string version, Dictionary<string, string> condition)
        {
            if (_client == null || string.IsNullOrEmpty(_client.SessionId) || _configuration.TwitchApi?.InnerAPI?.Helix?.EventSub == null) return null;
            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to subscribe to [{type}:{version}]");
                var response = await _configuration.TwitchApi.InnerAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: type, version: version, condition: condition,
                    method: EventSubTransportMethod.Websocket, websocketSessionId: _client.SessionId
                );

                if (response.Subscriptions == null || response.Total == 0)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Subscription request for [{type}:{version}] failed or returned empty data. Check scopes and broadcaster ID.");
                    return null;
                }
                if (response.Subscriptions.Any(d => d.Status.Contains("fail") || d.Status.Contains("error")))
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Subscription request for [{type}:{version}] resulted in error status: {string.Join(", ", response.Subscriptions.Select(d => d.Status))}");
                }
                return response;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Failed to subscribe to [{type}:{version}] due to an API error.", ex);
                return null;
            }
        }

        private void LogSubscriptionResult(CreateEventSubSubscriptionResponse result, string eventName)
        {
            if (result.Subscriptions != null)
            {
                foreach (var sub in result.Subscriptions)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Subscription to {eventName} [{sub.Type}:{sub.Version}] Status: {sub.Status}");
                }
            }
        }

        private void HandleDisconnection(string reason)
        {
            bool needsReconnect = false;
            lock (_connectionLock)
            {
                if (_client != null) 
                {
                    needsReconnect = true;
                    CleanupClientResources();
                }
            }
            OnStatusChanged(false, reason);

            if (needsReconnect)
            {
                AttemptReconnect();
            }
        }

        private void AttemptReconnect()
        {
            if (!_stopRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to reconnect in {ReconnectDelaySeconds} seconds...");
                Task.Run(async () =>
                {
                    CancellationToken token = _serviceCts?.Token ?? CancellationToken.None;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds), token);

                        if (!_stopRequested && !token.IsCancellationRequested)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnecting...");
                            Run(); 
                        }
                        else
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect cancelled (stop requested or token canceled).");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect delay was canceled.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during reconnect attempt execution.", ex);
                        OnStatusChanged(false, $"Reconnect failed: {ex.Message}");
                    }
                });
            }
            else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect skipped, service stop was requested.");
            }
        }

        private void StopInternal()
        {
            if (_serviceCts != null && !_serviceCts.IsCancellationRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Requesting cancellation for worker task...");
                try { _serviceCts.Cancel(); } catch (ObjectDisposedException) { } 
            }

            if (_workerTask != null && !_workerTask.IsCompleted)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Waiting for worker task to stop...");
                try
                {
                    bool completed = _workerTask.Wait(TimeSpan.FromSeconds(WorkerTaskShutdownWaitSeconds));
                    if (!completed)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Worker task did not complete within the timeout period.");
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Worker task stopped.");
                    }
                }
                catch (OperationCanceledException) { Logger.Log(LOGTYPE.INFO, ServiceName, "Worker task wait was canceled (expected)."); }
                catch (AggregateException ae) { ae.Handle(ex => { Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception occurred in worker task during shutdown.", ex); return true; }); }
                catch (ObjectDisposedException) { Logger.Log(LOGTYPE.WARNING, ServiceName, "Worker task was already disposed during wait."); }
            }
            _workerTask = null; // Обнуляем задачу
        }

        private void DisconnectClient()
        {
            if (_isConnected)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Requesting WebSocket disconnect...");
                try
                {
                    _ = _client?.DisconnectAsync(); 
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during client disconnect request.", ex);
                }
            }
            else if (_client != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Client exists but is not connected. Skipping disconnect request.");
            }
        }

        private void CleanupClientResources()
        {
            if (_client != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up EventSub client resources...");
                UnsubscribeFromClientEvents();
                _client = null; 
                Logger.Log(LOGTYPE.INFO, ServiceName, "EventSub client resources cleaned up.");
            }
        }

        private void CleanupNonClientResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up non-client resources (CTS, Semaphore, Queue)...");

            try { _serviceCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _serviceCts?.Dispose(); } catch (ObjectDisposedException) { }
            _serviceCts = null;

            try { _rewardSemaphore?.Dispose(); } catch (ObjectDisposedException) { }
            _rewardSemaphore = null;

            _rewardsQueue?.Clear();
            _rewardsQueue = null; // Обнуляем, как в вашем TwitchChatService

            Logger.Log(LOGTYPE.INFO, ServiceName, "Non-client resources cleaned up.");
        }

        private void CleanupResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Performing full resource cleanup...");
            StopInternal(); // Останавливаем worker
            lock (_connectionLock)
            {
                DisconnectClient(); // Отключаем клиент
                CleanupClientResources(); // Очищаем клиентские ресурсы
            }
            CleanupNonClientResources(); // Очищаем остальные ресурсы
            Logger.Log(LOGTYPE.INFO, ServiceName, "Full resource cleanup finished.");
        }

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
    }
}