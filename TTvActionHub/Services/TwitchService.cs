using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Managers;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Websockets;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.Api.Core.Enums;
using TTvActionHub.LuaTools.Services;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Clips.GetClips;

namespace TTvActionHub.Services
{
    public class TwitchService : IService, IUpdatableConfiguration
    {
        // --- IConfig impl ---
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public string ServiceName => nameof(TwitchService);
        public bool IsRunning { get; private set; } = false;

        // --- Connection checks

        public bool IsTwitchClientConnected => _twitchClient?.IsConnected ?? false;
        public bool IsEventSubClientConnected { get; private set; } = false;

        // --- Configuration constants ---
        private const int ReconnectDelaySeconds = 5;
        private const int MaxConcurrentEvents = 5;
        private const int WorkerQueuePollDelayMs = 100;
        private const int EventActionShutdownWaitSeconds = 3;
        private const int WorkerTaskShutdownWaitSeconds = 3;
        private const int PointsPerMinuteForViewers = 2;
        private const int PointsPerMessage = 1;
        private const int PointsPerClip = 10; 
        private const int ViewerPointsIntervalMinutes = 1;
        private const int ClipCheckIntervalMinutes = 5;
        private const string LastClipCheckTimeKey = "last_clip_check_time_utc";

        // --- Configuration ---
        private readonly LuaConfigManager _configManager;
        private readonly IConfig _configuration;

        // --- Twitch related fields ---
        private TwitchClient? _twitchClient;
        private EventSubWebsocketClient? _eventSubClient;

        // --- Events Handler ---
        public ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>? TwitchEvents { get; private set; }
        private ConcurrentQueue<(TwitchEvent Event, TwitchEventArgs Args)>? _eventsQueue;
        private SemaphoreSlim? _eventsSemaphore;
        private Task? _queueWorkerTask;

        // --- Points System ---
        private Timer? _viewerPointsTimer;
        private Timer? _clipPointsTimer; 

        // --- Thread safe objects --- 
        private readonly object _connectionLock = new();
        private volatile bool _stopRequested;
        private CancellationTokenSource? _serviceCts;
        private ConnectionCredentials? _credentials;

        public TwitchService(IConfig configuration, LuaConfigManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            TwitchEvents = _configManager.LoadTwitchEvents()
                ?? throw new InvalidOperationException($"Failed to load initial TwitchEvents configuration for {ServiceName}");
        }

        // --- Methods for LuaTools ---

        public void SendMessage(string message)
        {
            if (IsTwitchClientConnected && _twitchClient != null)
            {
                _twitchClient.SendMessage(_configuration.Login, message); 
            }
            else
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Cannot send message: Twitch Chat client is not connected.");
            }
        }
        
        public void SendWhisper(string target, string message)
        {
            if (IsTwitchClientConnected && _twitchClient != null)
            {
                _twitchClient.SendWhisper(target, message);
            }
            else
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Cannot send whisper: Twitch Chat client is not connected.");
            }
        }

        public int? GetEventCost(string eventName)
        {
            if (TwitchEvents == null) return null;
            var result = TwitchEvents.TryGetValue((eventName, TwitchTools.TwitchEventKind.Command), out var tevent);
            if (!result || tevent is null) return null;
            return tevent.Cost;
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
                    var needsTwitchClientReconnect = _twitchClient == null || !IsTwitchClientConnected;
                    var needsEventSubClientReconnect = _eventSubClient == null || !IsEventSubClientConnected;

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

                    StartViewerPointsTimer();
                    StartClipPointsTimer();
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

            StopViewerPointsTimer();
            StopClipPointsTimer();
            StopInternal(true);
            
            lock (_connectionLock)
            {
                DisconnectTwitchClient();
                DisconnectEventSubClient(); 
                CleanupTwitchClientResources();
                CleanupEventSubClientResources();
            }

            CleanupNonClientResources();

            Logger.Log(LOGTYPE.INFO, ServiceName, "Service stopped.");
            OnStatusChanged(false, "Service stopped by request.");
        }

        private void StopInternal(bool waitWorker)
        {
            if (_serviceCts is { IsCancellationRequested: false })
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Requesting cancellation for event queue worker task...");
                try { _serviceCts.Cancel(); }
                catch (ObjectDisposedException) { /* Expected */ }
            }
            
            if (waitWorker && _queueWorkerTask is { IsCompleted: false })
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
            _twitchClient.OnMessageReceived += TwitchClient_OnMessageReceived;
            _twitchClient.OnDisconnected += TwitchClient_OnDisconnected;
            _twitchClient.OnConnected += TwitchClient_OnConnected;
            _twitchClient.OnError += TwitchClient_OnError;
            if (_configuration.LogState) 
                _twitchClient.OnLog += TwitchClient_OnLog;
        }

        private void UnsubscribeFromTwitchClientEvents()
        {
            if (_twitchClient == null) return;
            _twitchClient.OnChatCommandReceived -= TwitchClient_OnChatCommandReceived;
            _twitchClient.OnConnectionError -= TwitchClient_OnConnectionError;
            _twitchClient.OnMessageReceived -= TwitchClient_OnMessageReceived;
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
            if (!IsTwitchClientConnected) return;
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

        private void DisconnectEventSubClient()
        {
            if (!IsEventSubClientConnected || _eventSubClient == null) return;
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

                            var eventTask = Task.Run(() =>
                            {
                                var eventIdentifier = $"event '{eventDataToProcess.Event.Name}' ({eventDataToProcess.Event.Kind}) for {eventDataToProcess.Args.Sender}";
                                try
                                {
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Executing {eventIdentifier}...");
                                    eventDataToProcess.Event.Execute(eventDataToProcess.Args);
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Finished {eventIdentifier}.");
                                    if (eventDataToProcess.Event.Cost > 0 && eventDataToProcess.Args.Sender != _configuration.Login)
                                    {
                                        _ = AddPointsToUserAsync(eventDataToProcess.Args.Sender, -eventDataToProcess.Event.Cost, "using a command with a cost");
                                        Logger.Log(LOGTYPE.INFO, ServiceName, $"Consuming {eventDataToProcess.Event.Cost} points from user: {eventDataToProcess.Args.Sender}");
                                    }
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
            if (_twitchClient == null) return;
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

        private void CleanupEventSubClientResources()
        {
            if (_eventSubClient == null) return;
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
            StopInternal(false); 

            lock (_connectionLock) 
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
            var currentServiceRunningStatus = IsRunning; 

            if (currentServiceRunningStatus)
            {
                message = IsTwitchClientConnected switch
                {
                    true when IsEventSubClientConnected => "All clients connected.",
                    true => "Twitch Chat connected, EventSub disconnected.",
                    _ => IsEventSubClientConnected
                        ? "EventSub connected, Twitch Chat disconnected."
                        : "All clients disconnected."
                };

                if (!string.IsNullOrEmpty(specificMessage))
                {
                    message = $"{specificMessage} ({message})";
                }
            }
            else 
            {
                message = "Service not running.";
                if (!string.IsNullOrEmpty(specificMessage))
                {
                    message = $"{specificMessage}"; 
                }
                else if (_stopRequested)
                {
                    message = "Service stopping...";
                }
            }

            OnStatusChanged(currentServiceRunningStatus && (IsTwitchClientConnected || IsEventSubClientConnected), message);
        }

        // --- Points System Methods ---
        private void StartViewerPointsTimer()
        {
            if (_viewerPointsTimer != null) return;
            _viewerPointsTimer = new Timer(AwardPointsToViewers, null, TimeSpan.Zero, TimeSpan.FromMinutes(ViewerPointsIntervalMinutes));
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Viewer points timer started. Interval: {ViewerPointsIntervalMinutes} min.");
        }

        private void StartClipPointsTimer()
        {
            if (_clipPointsTimer == null && !string.IsNullOrEmpty(_configuration.ID))
            {
                _clipPointsTimer = new Timer(CheckForNewClipsAndAwardPoints, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(ClipCheckIntervalMinutes)); // Небольшая задержка перед первым запуском
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Clip points timer started. Interval: {ClipCheckIntervalMinutes} min.");
            }
            else if (string.IsNullOrEmpty(_configuration.ID))
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Cannot start Clip points timer: TwitchApi or Broadcaster ID is not configured.");
            }
        }

        private void StopViewerPointsTimer()
        {
            _viewerPointsTimer?.Dispose();
            _viewerPointsTimer = null;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Viewer points timer stopped.");
        }

        private void StopClipPointsTimer()
        {
            _clipPointsTimer?.Dispose();
            _clipPointsTimer = null;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Clip points timer stopped.");
        }

        private async void AwardPointsToViewers(object? state)
        {
            if (!IsRunning || !IsTwitchClientConnected || string.IsNullOrEmpty(_configuration.ID))
            {
                return;
            }

            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to award points to viewers...");
            try
            {
                var broadcasterId = _configuration.ID;
                GetChattersResponse? response;
                try
                {
                    response = await _configuration.TwitchApi.InnerApi.Helix.Chat.GetChattersAsync(broadcasterId, broadcasterId);
                }
                catch (Exception apiEx) 
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to get chatters from Twitch API.", apiEx);
                    return;
                }

                if (response?.Data != null && response.Data.Length != 0)
                {
                    var awardedCount = 0;
                    foreach (var chatter in response.Data)
                    {
                        if (string.Equals(chatter.UserLogin, _configuration.Login, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        await AddPointsToUserAsync(chatter.UserLogin, PointsPerMinuteForViewers, "viewing");
                        awardedCount++;
                    }
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Awarded {PointsPerMinuteForViewers} points to {awardedCount} active chatters.");
                }
                else
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "No chatters found to award points to or API error.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error in AwardPointsToViewers.", ex);
            }
        }

        private async void CheckForNewClipsAndAwardPoints(object? state)
        {
            if (!IsRunning || string.IsNullOrEmpty(_configuration.ID))
            {
                return;
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "Checking for new clips...");

            try
            {
                var lastCheckTimeUtc = await Container.GetValueAsync<DateTime?>(LastClipCheckTimeKey) ?? DateTime.Today;
                //string startedAtFilter = lastCheckTimeUtc.ToString("o");

                GetClipsResponse? clipsResponse;
                try
                {
                    clipsResponse = await _configuration.TwitchApi.InnerApi.Helix.Clips.GetClipsAsync(
                        broadcasterId: _configuration.ID,
                        startedAt: lastCheckTimeUtc
                    );
                }
                catch (Exception apiEx)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to get clips from Twitch API.", apiEx);
                    return;
                }

                if (clipsResponse?.Clips != null && clipsResponse.Clips.Length != 0)
                {
                    lastCheckTimeUtc = DateTime.UtcNow;
                    var sortedClips = clipsResponse.Clips.OrderBy(c => c.CreatedAt).ToList();

                    foreach (var clip in sortedClips)
                    {
                        if (!string.IsNullOrWhiteSpace(clip.CreatorName))
                        {
                            if (string.Equals(clip.CreatorName, _configuration.Login, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Log(LOGTYPE.INFO, ServiceName, $"Clip {clip.Id} by {clip.CreatorName} (bot/channel) skipped for points.");
                            }
                            else
                            {
                                await AddPointsToUserAsync(clip.CreatorName, PointsPerClip, $"creating clip ({clip.Id[..8]}...)");
                            }
                        }
                        else
                        {
                            Logger.Log(LOGTYPE.WARNING, ServiceName, $"Clip {clip.Id} has no CreatorName. Cannot award points.");
                        }
                    }
                    await Container.InsertValueAsync(LastClipCheckTimeKey, lastCheckTimeUtc);
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Last clip check time updated to: {lastCheckTimeUtc:o}");
                }
                else
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "No new clips found since last check or API error.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error in CheckForNewClipsAndAwardPoints.", ex);
            }
        }

        public async Task AddPointsToUserAsync(string username, int pointsToAdd, string reason)
        {
            if (string.IsNullOrWhiteSpace(username) || pointsToAdd <= 0) return;

            var currentPoints = await GetPointsFromUser(username);
            var newPoints = currentPoints + pointsToAdd;
            await UpdateUserPoints(username, newPoints);
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Added {pointsToAdd} points to {username} for {reason}. Total: {newPoints}");
        }

        public static async Task<int> GetPointsFromUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return 0;
            var userPointsKey = $"user_points_{username.ToLower()}";
            var currentPoints = await Container.GetValueAsync<int?>(userPointsKey) ?? 0;
            return currentPoints;
        }

        public static async Task UpdateUserPoints(string username, int newValue)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            var userPointsKey = $"user_points_{username.ToLower()}";
            await Container.InsertValueAsync(userPointsKey, newValue);
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
                if (_configManager.LoadTwitchEvents() is { } events)
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
            if (string.IsNullOrEmpty(_configuration.ID)) 
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot register EventSub topics: Broadcaster User ID is missing in configuration.");
                return false;
            }
           
            var allSuccess = true;
            
            var condition = new Dictionary<string, string> { { "broadcaster_user_id", _configuration.ID } };

            var rewardResult = await SubscribeToEventAsync("channel.channel_points_custom_reward_redemption.add", "1", condition); // channel points reward
            if (!rewardResult) allSuccess = false;

            return allSuccess;
        }

        private async Task<bool> SubscribeToEventAsync(string type, string version, Dictionary<string, string> condition)
        {
            if (_eventSubClient == null || string.IsNullOrEmpty(_eventSubClient.SessionId) || _configuration.TwitchApi.InnerApi.Helix.EventSub == null)
            {
                return false;
            }
            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to subscribe to [{type}:{version}]");
                var response = await _configuration.TwitchApi.InnerApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: type, version: version, condition: condition,
                    method: EventSubTransportMethod.Websocket, websocketSessionId: _eventSubClient.SessionId
                );
                if (response?.Subscriptions == null || response.Subscriptions.Length == 0)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"EventSub subscription request for [{type}:{version}] failed or returned empty data. Check scopes and broadcaster ID. Cost: {response?.TotalCost}, MaxCost: {response?.MaxTotalCost}");
                    return false;
                }
                var subscriptionSuccessful = true;
                foreach (var sub in response.Subscriptions)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"EventSub subscription to [{sub.Type} v{sub.Version}] Status: {sub.Status}. Cost: {sub.Cost}. ID: {sub.Id}");
                    if (sub.Status is "enabled" or "webhook_callback_verification_pending") continue; // "enabled" for websocket
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"EventSub subscription for [{sub.Type}] has status: {sub.Status}. Expected 'enabled'.");
                        
                    if (sub.Status.Contains("fail") || sub.Status.Contains("revoked") || sub.Status.Contains("error"))
                    {
                        subscriptionSuccessful = false;
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
            
            if (TwitchEvents == null || !TwitchEvents.TryGetValue((cmdName, TwitchTools.TwitchEventKind.Command), out var twitchEvent))
            {
                return;
            }

            if (!twitchEvent.Executable)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Command '{cmdName}' from {chatCommand.ChatMessage.Username} cannot be executed right now (e.g., on cooldown).");
                return;
            }

            var senderUsername = chatCommand.ChatMessage.Username;
            var cmdArgStr = chatCommand.ArgumentsAsString.Replace("\U000e0000", "").Trim();
            var cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');

            var userLevel = TwitchTools.ParseFromTwitchLib(
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

        private void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            var chatMessage = e.ChatMessage;
            if (chatMessage.Message.Length < 10) return;
            AddPointsToUserAsync(chatMessage.Username, PointsPerMessage, "messages").GetAwaiter().GetResult();
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

            if (TwitchEvents == null || !TwitchEvents.TryGetValue((rewardTitle, TwitchTools.TwitchEventKind.TwitchReward), out var twitchEvent))
            {
                return Task.CompletedTask;
            }

            var senderUsername = redemptionEvent.UserName;
            var rewardArgsStr = redemptionEvent.UserInput.Trim();
            var rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');

            var eventArgs = new TwitchEventArgs
            {
                Sender = senderUsername,
                Args = rewardArgs,
                Permission = TwitchTools.PermissionLevel.Viewer 
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
                            _eventSubClient = new EventSubWebsocketClient();
                            SubscribeToEventSubClientEvents();
                            _ = _eventSubClient.ConnectAsync();

                        }
                        else if (clientToReconnect == null) 
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting full service Run for reconnect.");
                            Run(); 
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
