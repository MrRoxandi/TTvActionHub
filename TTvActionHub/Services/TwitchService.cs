using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services;
using TTvActionHub.Managers;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using LogType = TTvActionHub.Logs.LogType;
using TTvActionHub.Services.Interfaces;

namespace TTvActionHub.Services;

public class TwitchService : IService, IUpdatableConfiguration, IPointsService
{
    // --- IConfig impl ---
    public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
    public string ServiceName => nameof(TwitchService);
    public bool IsRunning { get; private set; }

    // --- Db for Twitch users --- 

    private readonly PointsManager _db;

    // --- Connection checks ---

    public bool IsTwitchClientConnected => _twitchClient?.IsConnected ?? false;
    public bool IsEventSubClientConnected { get; private set; }

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
    private EventSubWebsocketClient? _eventSubClient;

    /*private readonly TwitchAPI twitchAPI;*/
    private TwitchClient? _twitchClient;

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
        /*twitchAPI = _configuration.TwitchApi.InnerApi;*/
        _db = new PointsManager("Twitch");
        TwitchEvents = _configManager.LoadTwitchEvents()
                       ?? throw new InvalidOperationException(
                           $"Failed to load initial TwitchEvents configuration for {ServiceName}");
    }

    // --- Methods for LuaTools ---

    public void SendMessage(string message)
    {
        if (IsTwitchClientConnected && _twitchClient != null)
        {
            _twitchClient.SendMessage(_configuration.Login, message);
            return;
        }

        Logger.Log(LogType.Warning, ServiceName, "Cannot send message: Twitch Chat client is not connected.");
    }

    public void SendWhisper(string target, string message)
    {
        if (IsTwitchClientConnected && _twitchClient != null)
        {
            _twitchClient.SendWhisper(target, message);
            return;
        }

        Logger.Log(LogType.Warning, ServiceName, "Cannot send whisper: Twitch Chat client is not connected.");
    }

    public long? GetEventCost(string eventName)
    {
        if (TwitchEvents == null) return null;
        var result = TwitchEvents.TryGetValue((eventName, TwitchTools.TwitchEventKind.Command), out var tEvent);
        if (!result || tEvent is null) return null;
        return tEvent.Cost;
    }

    // --- Running and Stopping service --- 
#region init
    public void Run()
    {
        _stopRequested = false;
        Logger.Log(LogType.Info, ServiceName, "Starting service ...");
        lock (_connectionLock)
        {
            if (IsRunning && (IsTwitchClientConnected || IsEventSubClientConnected))
            {
                Logger.Log(LogType.Warning, ServiceName,
                    "Service is already marked as running and at least one client is connected.");
                var needsTwitchClientReconnect = _twitchClient == null || !IsTwitchClientConnected;
                var needsEventSubClientReconnect = _eventSubClient == null || !IsEventSubClientConnected;

                if (!needsTwitchClientReconnect && !needsEventSubClientReconnect)
                {
                    OnStatusChanged(true, "Service already running and clients connected.");
                    return;
                }

                Logger.Log(LogType.Info, ServiceName,
                    "Service marked as running, but one or more clients need connection. Proceeding...");
            }

            if (_twitchClient != null && !IsTwitchClientConnected)
            {
                Logger.Log(LogType.Info, ServiceName,
                    "TwitchClient instance exists but not connected. Cleaning up before restart attempt.");
                CleanupTwitchClientResources();
            }

            if (_eventSubClient != null && !IsEventSubClientConnected)
            {
                Logger.Log(LogType.Info, ServiceName,
                    "EventSubClient instance exists but not connected. Cleaning up before restart attempt.");
                CleanupEventSubClientResources();
            }

            try
            {
                Logger.Log(LogType.Info, ServiceName, "Initializing resources and clients...");

                _serviceCts ??= new CancellationTokenSource();
                _eventsQueue ??= new ConcurrentQueue<(TwitchEvent, TwitchEventArgs)>();
                _eventsSemaphore ??= new SemaphoreSlim(MaxConcurrentEvents, MaxConcurrentEvents);

                if (_twitchClient == null)
                {
                    Logger.Log(LogType.Info, ServiceName, "Initializing Twitch Chat client...");
                    _twitchClient = new TwitchClient();
                    _credentials = new ConnectionCredentials(_configuration.Login, _configuration.Token);
                    SubscribeToTwitchClientEvents();
                    _twitchClient.Initialize(_credentials, _configuration.Login);
                }

                if (!IsTwitchClientConnected)
                {
                    Logger.Log(LogType.Info, ServiceName, "Connecting Twitch Chat client...");
                    _twitchClient.Connect();
                }

                if (_eventSubClient == null)
                {
                    Logger.Log(LogType.Info, ServiceName, "Initializing EventSub client...");
                    _eventSubClient = new EventSubWebsocketClient();
                    SubscribeToEventSubClientEvents();
                }

                if (!IsEventSubClientConnected)
                {
                    Logger.Log(LogType.Info, ServiceName, "Connecting EventSub client...");
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
                Logger.Log(LogType.Error, ServiceName, "Failed to start or connect one or more clients.", ex);
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
            Logger.Log(LogType.Info, ServiceName, "Stop already requested.");
            return;
        }

        _stopRequested = true;
        IsRunning = false;
        Logger.Log(LogType.Info, ServiceName, "Stopping service requested...");

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

        Logger.Log(LogType.Info, ServiceName, "Service stopped.");
        OnStatusChanged(false, "Service stopped by request.");
    }

    private void StopInternal(bool waitWorker)
    {
        if (_serviceCts is { IsCancellationRequested: false })
        {
            Logger.Log(LogType.Info, ServiceName, "Requesting cancellation for event queue worker task...");
            try
            {
                _serviceCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                /* Expected */
            }
        }

        if (waitWorker && _queueWorkerTask is { IsCompleted: false })
        {
            Logger.Log(LogType.Info, ServiceName, "Waiting for event queue worker task to stop...");
            try
            {
                if (!_queueWorkerTask.Wait(TimeSpan.FromSeconds(WorkerTaskShutdownWaitSeconds)))
                    Logger.Log(LogType.Warning, ServiceName,
                        "Event queue worker task did not complete within the timeout period.");
                else
                    Logger.Log(LogType.Info, ServiceName, "Event queue worker task stopped.");
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LogType.Info, ServiceName, "Event queue worker task wait was canceled (expected).");
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    Logger.Log(LogType.Error, ServiceName, "Exception occurred in worker task during shutdown.", ex);
                    return true;
                });
            }
            catch (ObjectDisposedException)
            {
                Logger.Log(LogType.Warning, ServiceName, "Worker task was already disposed during wait.");
            }
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
        _eventSubClient.ChannelPointsCustomRewardRedemptionAdd +=
            EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler;
    }

    private void UnsubscribeFromEventSubClientEvents()
    {
        if (_eventSubClient == null) return;
        _eventSubClient.WebsocketConnected -= EventSubClient_WebsocketConnectedHandler;
        _eventSubClient.WebsocketDisconnected -= EventSubClient_WebsocketDisconnectedHandler;
        _eventSubClient.ErrorOccurred -= EventSubClient_ErrorOccurredHandler;
        _eventSubClient.ChannelPointsCustomRewardRedemptionAdd -=
            EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler;
    }

    private void DisconnectTwitchClient()
    {
        if (!IsTwitchClientConnected) return;
        Logger.Log(LogType.Info, ServiceName, "Disconnecting Twitch Chat client...");
        try
        {
            _twitchClient?.Disconnect();
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Error during Twitch Chat client disconnect request.", ex);
        }
    }

    private void DisconnectEventSubClient()
    {
        if (!IsEventSubClientConnected || _eventSubClient == null) return;
        Logger.Log(LogType.Info, ServiceName, "Requesting EventSub WebSocket disconnect...");
        try
        {
            _ = _eventSubClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Error during EventSub client disconnect request.", ex);
        }

        IsEventSubClientConnected = false;
    }
#endregion 
   
#region EventQueue
    private void EnqueueTwitchEvent(TwitchEvent twitchEvent, TwitchEventArgs eventArgs)
    {
        if (_eventsQueue == null || _serviceCts?.IsCancellationRequested == true)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"Cannot enqueue {twitchEvent.Name}: Queue is null or service is stopping.");
            return;
        }

        Logger.Log(LogType.Info, ServiceName, $"Queueing {twitchEvent.Name} from {eventArgs.Sender}.");
        _eventsQueue.Enqueue((twitchEvent, eventArgs));
    }

    private async Task ProcessEventsQueueAsync(CancellationToken cancellationToken)
    {
        Logger.Log(LogType.Info, ServiceName, "Event processing worker started.");
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
                        Logger.Log(LogType.Error, ServiceName,
                            "Events semaphore is null, cannot process event. This is a critical error.");
                        await Task.Delay(WorkerQueuePollDelayMs * 10, cancellationToken);
                        _eventsQueue.Enqueue(eventDataToProcess);
                        continue;
                    }

                    try
                    {
                        await _eventsSemaphore.WaitAsync(cancellationToken);

                        var eventTask = Task.Run(() =>
                        {
                            var eventIdentifier =
                                $"event '{eventDataToProcess.Event.Name}' ({eventDataToProcess.Event.Kind}) for {eventDataToProcess.Args.Sender}";
                            try
                            {
                                if (eventDataToProcess.Event.Cost > 0 &&
                                    eventDataToProcess.Args.Sender != _configuration.Login)
                                {
                                    var points = GetPointsAsync(eventDataToProcess.Args.Sender).GetAwaiter()
                                        .GetResult();
                                    if (points < eventDataToProcess.Event.Cost) return;
                                }

                                Logger.Log(LogType.Info, ServiceName, $"Executing {eventIdentifier}...");
                                eventDataToProcess.Event.Execute(eventDataToProcess.Args);
                                Logger.Log(LogType.Info, ServiceName, $"Finished {eventIdentifier}.");
                                if (eventDataToProcess.Event.Cost <= 0 ||
                                    eventDataToProcess.Args.Sender == _configuration.Login) return;
                                _ = AddPointsAsync(eventDataToProcess.Args.Sender,
                                    -eventDataToProcess.Event.Cost);
                                Logger.Log(LogType.Info, ServiceName,
                                    $"Consuming {eventDataToProcess.Event.Cost} points from user: {eventDataToProcess.Args.Sender}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error, ServiceName, $"Error executing {eventIdentifier}", ex);
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
                        Logger.Log(LogType.Info, ServiceName,
                            "Operation canceled while waiting for semaphore or starting event task.");
                        _eventsQueue.Enqueue(eventDataToProcess);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, ServiceName,
                            $"Error dispatching event '{eventDataToProcess.Event.Name}':", ex);
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
            Logger.Log(LogType.Info, ServiceName, "Event processing loop was canceled.");
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Unhandled error in event processing loop.", ex);
        }
        finally
        {
            Logger.Log(LogType.Info, ServiceName,
                $"Event worker loop finished. Waiting for {runningEventTasks.Count(t => !t.IsCompleted)} running event action(s) to complete...");
            try
            {
                await Task.WhenAll([.. runningEventTasks])
                    .WaitAsync(TimeSpan.FromSeconds(EventActionShutdownWaitSeconds), CancellationToken.None);
                Logger.Log(LogType.Info, ServiceName, "All tracked event actions finished or timed out.");
            }
            catch (TimeoutException)
            {
                Logger.Log(LogType.Warning, ServiceName,
                    "Timeout waiting for running event actions to complete upon shutdown.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Warning, ServiceName,
                    $"Exception while waiting for running event actions: {ex.Message}");
            }

            Logger.Log(LogType.Info, ServiceName, "Event processing worker stopped.");
        }
    }
#endregion    
    
#region cleanUp
    private void CleanupTwitchClientResources()
    {
        if (_twitchClient == null) return;
        Logger.Log(LogType.Info, ServiceName, "Cleaning up Twitch Chat client resources...");
        UnsubscribeFromTwitchClientEvents();
        if (IsTwitchClientConnected) _twitchClient.Disconnect();
        _twitchClient = null;
        _credentials = null;
        Logger.Log(LogType.Info, ServiceName, "Twitch Chat client resources cleaned up.");
    }

    private void CleanupEventSubClientResources()
    {
        if (_eventSubClient == null) return;
        Logger.Log(LogType.Info, ServiceName, "Cleaning up EventSub client resources...");
        UnsubscribeFromEventSubClientEvents();
        if (IsEventSubClientConnected) _ = _eventSubClient.DisconnectAsync();
        _eventSubClient = null;
        IsEventSubClientConnected = false;
        Logger.Log(LogType.Info, ServiceName, "EventSub client resources cleaned up.");
    }

    private void CleanupNonClientResources()
    {
        Logger.Log(LogType.Info, ServiceName, "Cleaning up non-client resources (CTS, Semaphore, Queue)...");

        try
        {
            _serviceCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            _serviceCts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }

        _serviceCts = null;

        try
        {
            _eventsSemaphore?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }

        _eventsSemaphore = null;

        _eventsQueue = null;
        Logger.Log(LogType.Info, ServiceName, "Non-client resources cleaned up.");
    }

    private void CleanupFullResources()
    {
        Logger.Log(LogType.Info, ServiceName, "Performing full resource cleanup...");
        StopInternal(false);

        lock (_connectionLock)
        {
            DisconnectTwitchClient();
            DisconnectEventSubClient();
            CleanupTwitchClientResources();
            CleanupEventSubClientResources();
        }

        CleanupNonClientResources();
        Logger.Log(LogType.Info, ServiceName, "Full resource cleanup finished.");
    }
#endregion
    
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

            if (!string.IsNullOrEmpty(specificMessage)) message = $"{specificMessage} ({message})";
        }
        else
        {
            message = "Service not running.";
            if (!string.IsNullOrEmpty(specificMessage))
                message = $"{specificMessage}";
            else if (_stopRequested) message = "Service stopping...";
        }

        OnStatusChanged(currentServiceRunningStatus && (IsTwitchClientConnected || IsEventSubClientConnected),
            message);
    }

    // --- Points System Methods ---
    private void StartViewerPointsTimer()
    {
        Logger.Log(LogType.Info, ServiceName, "Viewer points timer is disabled in code...");
        /*if (_viewerPointsTimer != null) return;
        _viewerPointsTimer = new Timer(AwardPointsToViewers, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(ViewerPointsIntervalMinutes));
        Logger.Log(LogType.Info, ServiceName, $"Viewer points timer started. Interval: {ViewerPointsIntervalMinutes} min.");*/
    }

    private void StartClipPointsTimer()
    {
        if (_clipPointsTimer == null && !string.IsNullOrEmpty(_configuration.Id))
        {
            _clipPointsTimer = new Timer(CheckForNewClipsAndAwardPoints, null, TimeSpan.FromSeconds(20),
                TimeSpan.FromMinutes(ClipCheckIntervalMinutes));
            Logger.Log(LogType.Info, ServiceName,
                $"Clip points timer started. Interval: {ClipCheckIntervalMinutes} min.");
        }
        else if (string.IsNullOrEmpty(_configuration.Id))
        {
            Logger.Log(LogType.Warning, ServiceName,
                "Cannot start Clip points timer: TwitchApi or Broadcaster ID is not configured.");
        }
    }

    private void StopViewerPointsTimer()
    {
        _viewerPointsTimer?.Dispose();
        _viewerPointsTimer = null;
        Logger.Log(LogType.Info, ServiceName, "Viewer points timer stopped.");
    }

    private void StopClipPointsTimer()
    {
        _clipPointsTimer?.Dispose();
        _clipPointsTimer = null;
        Logger.Log(LogType.Info, ServiceName, "Clip points timer stopped.");
    }

    private async void AwardPointsToViewers(object? state) // disabled for now 
    {
        if (!IsRunning || !IsTwitchClientConnected || string.IsNullOrEmpty(_configuration.Id)) return;

        Logger.Log(LogType.Info, ServiceName, "Attempting to award points to viewers...");
        try
        {
            var broadcasterId = _configuration.Id;
            GetChattersResponse? response;
            try
            {
                response = await _configuration.TwitchApi.InnerApi.Helix.Chat.GetChattersAsync(broadcasterId,
                    broadcasterId);
            }
            catch (Exception apiEx)
            {
                Logger.Log(LogType.Error, ServiceName, "Failed to get chatters from Twitch API.", apiEx);
                return;
            }

            if (response?.Data != null && response.Data.Length != 0)
            {
                var awardedCount = 0;
                foreach (var chatter in response.Data)
                {
                    if (string.Equals(chatter.UserLogin, _configuration.Login,
                            StringComparison.OrdinalIgnoreCase)) continue;
                    await AddPointsAsync(chatter.UserLogin, PointsPerMinuteForViewers);
                    awardedCount++;
                }

                Logger.Log(LogType.Info, ServiceName,
                    $"Awarded {PointsPerMinuteForViewers} points to {awardedCount} active chatters.");
            }
            else
            {
                Logger.Log(LogType.Info, ServiceName, "No chatters found to award points to or API error.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Error in AwardPointsToViewers.", ex);
        }
    }

    private async void CheckForNewClipsAndAwardPoints(object? state)
    {
        if (!IsRunning || string.IsNullOrEmpty(_configuration.Id)) return;
        Logger.Log(LogType.Info, ServiceName, "Checking for new clips...");
        var cursor = string.Empty;
        var lastCheckTimeUtc = await Container.Storage!.GetItemAsync<DateTime?>(LastClipCheckTimeKey)
                               ?? DateTime.Today.AddYears(-3);
        do
        {
            try
            {
                var result = await _configuration.TwitchApi.InnerApi.Helix.Clips.GetClipsAsync(
                    broadcasterId: _configuration.Id,
                    accessToken: _configuration.Token,
                    startedAt: lastCheckTimeUtc,
                    endedAt: DateTime.Today,
                    after: string.IsNullOrEmpty(cursor) ? null : cursor,
                    first: 100
                );

                if (result == null)
                {
                    Logger.Log(LogType.Error, ServiceName, "Api error, get empty response.");
                    break;
                }

                if (result.Clips is { Length: > 0 } clips)
                {
                    Logger.Log(LogType.Info, ServiceName, $"Processing points reward for {clips.Length} clips...");
                    foreach (var clip in clips.OrderByDescending(t => t.CreatedAt))
                    {
                        // Checking name
                        if (string.IsNullOrWhiteSpace(clip.CreatorName)) continue;
                        if (string.Equals(clip.CreatorName, _configuration.Login,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Log(LogType.Info, ServiceName,
                                $"Clip {clip.Id} by {clip.CreatorName} (bot/channel) skipped for points.");
                            continue;
                        }

                        Logger.Log(LogType.Info, ServiceName,
                            $"adding {PointsPerClip} point to {clip.CreatorName} for creating clip ({clip.Id[..8]})");
                        await AddPointsByIdAsync(clip.CreatorId, PointsPerClip);
                    }
                }
                else
                {
                    Logger.Log(LogType.Info, ServiceName, "No new clips found since last check.");
                }

                //
                cursor = result.Pagination.Cursor;
            }
            catch (Exception apiEx)
            {
                Logger.Log(LogType.Error, ServiceName, "Failed to get clips from Twitch API:", apiEx);
                return;
            }
        } while (!string.IsNullOrEmpty(cursor));

        await Container.Storage.AddOrUpdateItemAsync(LastClipCheckTimeKey, DateTime.UtcNow);
    }
    
    private async Task<bool> ModifyUserPoints(string username, long points, bool isAdding = true)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        var userId = await GetUserIdFromApi(username);
        if (userId is null)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Cannot modify points for '{username}': User ID not found.");
            return false;
        }

        var userExists = await _db.ContainsIdAsync(userId);
        if (userExists)
            return isAdding
                ? await _db.AddUserPointsByIdAsync(userId, points)
                : await _db.SetUserPointsByIdAsync(userId, points);
        await _db.CreateUserAsync(username, userId);
        return isAdding ?
            await _db.AddUserPointsByIdAsync(userId, points) :
            await _db.SetUserPointsByIdAsync(userId, points);
    }

    private async Task<bool> ModifyUserPointsById(string userId, long points, bool isAdding = true)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var userExists = await _db.ContainsIdAsync(userId);
        if (userExists)
            return isAdding
                ? await _db.AddUserPointsByIdAsync(userId, points)
                : await _db.SetUserPointsByIdAsync(userId, points);
        var username = await GetUserLoginFromApi(userId);
        if (username is null)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Cannot modify points for 'id: {userId}': Username not found.");
            return false;
        }
        await _db.CreateUserAsync(username, userId);

        return isAdding ?
            await _db.AddUserPointsByIdAsync(userId, points) :
            await _db.SetUserPointsByIdAsync(userId, points);
    }

    private async Task<string?> GetUserIdFromApi(string username)
    {
        var id = await _configuration.TwitchApi.InnerApi.Helix.Users.GetUsersAsync(logins: [username]);
        return id?.Users.First().Id;
    }

    private async Task<string?> GetUserLoginFromApi(string id)
    {
        var users = await _configuration.TwitchApi.InnerApi.Helix.Users.GetUsersAsync([id]);
        return users?.Users.First().Login;
    }

    public async Task<bool> AddPointsAsync(string username, long points) => await ModifyUserPoints(username, points);

    public async Task<bool> SetPointsAsync(string username, long points) =>
        await ModifyUserPoints(username, points, false);


    public async Task<long> GetPointsAsync(string username) => await _db.GetUserPointsAsync(username);

    public async Task<Dictionary<string, long>> GetAllUsersPointsAsync() => await _db.GetAllUsersPointsAsync();

    public async Task<string?> GetUserIdByNameAsync(string username)
    {
        var user = await _db.GetUserAsync(username);
        return user?.UserId ?? null;
    }

    public async Task<string?> GetUserNameByIdAsync(string userId)
    {
        var user = await _db.GetUserByIdAsync(userId);
        return user?.Username ?? null;
    }

    public async Task<bool> AddPointsByIdAsync(string userId, long points) => await ModifyUserPointsById(userId, points);

    public async Task<bool> SetPointsByIdAsync(string userId, long points) =>
        await ModifyUserPointsById(userId, points, false);

    public async Task<long> GetPointsByIdAsync(string userId) => await _db.GetUserPointsByIdAsync(userId); 
    
    // --- Status Changed event backend --- 
    private void OnStatusChanged(bool isRunning, string? message = null)
    {
        try
        {
            StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Error invoking StatusChanged event handler.", ex);
        }
    }

    public bool UpdateConfiguration()
    {
        Logger.Log(LogType.Info, ServiceName, "Attempting to update configuration...");
        try
        {
            if (_configManager.LoadTwitchEvents() is { } events)
            {
                TwitchEvents = events;
                Logger.Log(LogType.Info, ServiceName, "Configuration updated successfully.");
                return true;
            }

            Logger.Log(LogType.Warning, ServiceName, "Failed to update configuration: LoadTwitchEvents returned null.");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Failed to update configuration due to an error.", ex);
            return false;
        }
    }

    private async Task<bool> RegisterEventSubTopicsAsync()
    {
        if (_eventSubClient == null || string.IsNullOrEmpty(_eventSubClient.SessionId))
        {
            Logger.Log(LogType.Error, ServiceName,
                "Cannot register EventSub topics: Client is not connected or Session ID is missing.");
            return false;
        }

        if (string.IsNullOrEmpty(_configuration.Id))
        {
            Logger.Log(LogType.Error, ServiceName,
                "Cannot register EventSub topics: Broadcaster User ID is missing in configuration.");
            return false;
        }

        var allSuccess = true;

        var condition = new Dictionary<string, string> { { "broadcaster_user_id", _configuration.Id } };

        var rewardResult =
            await SubscribeToEventAsync("channel.channel_points_custom_reward_redemption.add", "1",
                condition); // channel points reward
        if (!rewardResult) allSuccess = false;

        return allSuccess;
    }

    private async Task<bool> SubscribeToEventAsync(string type, string version, Dictionary<string, string> condition)
    {
        if (_eventSubClient == null || string.IsNullOrEmpty(_eventSubClient.SessionId) ||
            _configuration.TwitchApi.InnerApi.Helix.EventSub == null) return false;
        try
        {
            Logger.Log(LogType.Info, ServiceName, $"Attempting to subscribe to [{type}:{version}]");
            var response = await _configuration.TwitchApi.InnerApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                type, version, condition,
                EventSubTransportMethod.Websocket, _eventSubClient.SessionId
            );
            if (response?.Subscriptions == null || response.Subscriptions.Length == 0)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"EventSub subscription request for [{type}:{version}] failed or returned empty data. Check scopes and broadcaster ID. Cost: {response?.TotalCost}, MaxCost: {response?.MaxTotalCost}");
                return false;
            }

            var subscriptionSuccessful = true;
            foreach (var sub in response.Subscriptions)
            {
                Logger.Log(LogType.Info, ServiceName,
                    $"EventSub subscription to [{sub.Type} v{sub.Version}] Status: {sub.Status}. Cost: {sub.Cost}. ID: {sub.Id}");
                if (sub.Status is "enabled" or "webhook_callback_verification_pending")
                    continue; // "enabled" for websocket
                Logger.Log(LogType.Warning, ServiceName,
                    $"EventSub subscription for [{sub.Type}] has status: {sub.Status}. Expected 'enabled'.");

                if (sub.Status.Contains("fail") || sub.Status.Contains("revoked") || sub.Status.Contains("error"))
                    subscriptionSuccessful = false;
            }

            if (!subscriptionSuccessful)
                Logger.Log(LogType.Error, ServiceName,
                    "One or more EventSub subscriptions for did not report 'enabled' status.");
            return subscriptionSuccessful;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName,
                $"Failed to subscribe to EventSub topic [{type}:{version}] due to an API error.", ex);
            return false;
        }
    }
    
#region --- TwitchLib Events Handler ---

    private void TwitchClient_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
    {
        var chatCommand = args.Command;
        var cmdName = chatCommand.CommandText;

        if (TwitchEvents == null ||
            !TwitchEvents.TryGetValue((cmdName, TwitchTools.TwitchEventKind.Command), out var twitchEvent)) return;

        if (!twitchEvent.Executable)
        {
            Logger.Log(LogType.Info, ServiceName,
                $"Command '{cmdName}' from {chatCommand.ChatMessage.Username} cannot be executed right now (e.g., on cooldown).");
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
        Logger.Log(LogType.Info, ServiceName, $"Twitch Chat client connected to '{_configuration.Login}'.");
        UpdateOverallStatus("Twitch Chat Connected");
    }

    private void TwitchClient_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        Logger.Log(LogType.Warning, ServiceName, "Twitch Chat client disconnected.");
        HandleClientDisconnection("Twitch Chat", "Disconnected");
    }

    private void TwitchClient_OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        Logger.Log(LogType.Error, ServiceName, $"Twitch Chat client connection error: {e.Error.Message}");
        HandleClientDisconnection("Twitch Chat", $"Connection Error: {e.Error.Message}");
    }

    private void TwitchClient_OnError(object? sender, OnErrorEventArgs e)
    {
        Logger.Log(LogType.Error, ServiceName, "TwitchLib (Chat Client) internal error.", e.Exception);
    }

    private void TwitchClient_OnLog(object? sender, OnLogArgs e)
    {
        Logger.Log(LogType.Info, $"{ServiceName} [TwitchClient Log]", e.Data);
    }

    private void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var chatMessage = e.ChatMessage;
        if (chatMessage.Message.Length < 10 || chatMessage.Username == _configuration.Login) return;
        Logger.Log(LogType.Info, ServiceName, $"Awarding {chatMessage.Username} with {PointsPerMessage} for message");
        _ = AddPointsAsync(chatMessage.DisplayName, PointsPerMessage);
    }

    private async Task EventSubClient_WebsocketConnectedHandler(object? sender, WebsocketConnectedArgs args)
    {
        IsEventSubClientConnected = true;
        Logger.Log(LogType.Info, ServiceName,
            $"EventSub WebSocket connected. IsRequestedReconnect: {args.IsRequestedReconnect}");

        if (!args.IsRequestedReconnect && _eventSubClient != null)
        {
            Logger.Log(LogType.Info, ServiceName, "Attempting to subscribe to EventSub topics...");
            try
            {
                var success = await RegisterEventSubTopicsAsync();
                if (success)
                {
                    Logger.Log(LogType.Info, ServiceName, "Successfully subscribed to EventSub topics.");
                    UpdateOverallStatus("EventSub Connected and Subscribed");
                }
                else
                {
                    Logger.Log(LogType.Error, ServiceName, "Failed to subscribe to one or more EventSub topics.");
                    UpdateOverallStatus("EventSub Connected but Subscription Failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, "Error during EventSub topic subscription.", ex);
                UpdateOverallStatus($"EventSub Connected but Subscription Error: {ex.Message}");
            }
        }
        else
        {
            Logger.Log(LogType.Info, ServiceName,
                "EventSub reconnected. Assuming subscriptions persist or will be re-established by TwitchLib if needed.");
            UpdateOverallStatus("EventSub Reconnected");
        }
    }

    private Task EventSubClient_WebsocketDisconnectedHandler(object? sender, EventArgs args)
    {
        IsEventSubClientConnected = false;
        Logger.Log(LogType.Warning, ServiceName, "EventSub WebSocket disconnected.");
        HandleClientDisconnection("EventSub", "WebSocket Disconnected");
        return Task.CompletedTask;
    }

    private Task EventSubClient_ErrorOccurredHandler(object? sender, ErrorOccuredArgs args)
    {
        Logger.Log(LogType.Error, ServiceName, $"EventSub client error: {args.Message}", args.Exception);
        return Task.CompletedTask;
    }
#endregion

    private Task EventSubClient_ChannelPointsCustomRewardRedemptionAddHandler(object? sender,
        ChannelPointsCustomRewardRedemptionArgs args)
    {
        var redemptionEvent = args.Notification.Payload.Event;
        var rewardTitle = redemptionEvent.Reward.Title;

        if (TwitchEvents == null || !TwitchEvents.TryGetValue((rewardTitle, TwitchTools.TwitchEventKind.TwitchReward),
                out var twitchEvent)) return Task.CompletedTask;

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
        if (clientName == "EventSub") IsEventSubClientConnected = false;

        UpdateOverallStatus($"{clientName} Disconnected: {reason}");

        if (!_stopRequested)
            AttemptReconnect(clientName);
        else
            Logger.Log(LogType.Info, ServiceName, $"Reconnect for {clientName} skipped, service stop was requested.");
    }

    private void AttemptReconnect(string? clientToReconnect = null)
    {
        if (_stopRequested) return;
        Logger.Log(LogType.Info, ServiceName,
            $"Attempting to reconnect {clientToReconnect ?? "service"} in {ReconnectDelaySeconds} seconds...");

        Task.Run(async () =>
        {
            var token = _serviceCts?.Token ?? CancellationToken.None;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds), token);

                if (_stopRequested || token.IsCancellationRequested)
                {
                    Logger.Log(LogType.Info, ServiceName, "Reconnect cancelled (stop requested or token canceled).");
                    return;
                }

                Logger.Log(LogType.Info, ServiceName, $"Reconnecting {clientToReconnect ?? "service"}...");
                lock (_connectionLock) // Блокировка для изменения состояния клиентов
                {
                    switch (clientToReconnect)
                    {
                        case "Twitch Chat" when !IsTwitchClientConnected:
                            Logger.Log(LogType.Info, ServiceName,
                                "Attempting to reconnect Twitch Chat client specifically.");
                            CleanupTwitchClientResources();
                            _twitchClient = new TwitchClient();
                            _credentials = new ConnectionCredentials(_configuration.Login, _configuration.Token);
                            _twitchClient.Initialize(_credentials, _configuration.Login);
                            SubscribeToTwitchClientEvents();
                            _twitchClient.Connect();
                            break;
                        case "EventSub" when !IsEventSubClientConnected:
                            Logger.Log(LogType.Info, ServiceName,
                                "Attempting to reconnect EventSub client specifically.");
                            CleanupEventSubClientResources();
                            _eventSubClient = new EventSubWebsocketClient();
                            SubscribeToEventSubClientEvents();
                            _ = _eventSubClient.ConnectAsync();
                            break;
                        case null:
                            Logger.Log(LogType.Info, ServiceName, "Attempting full service Run for reconnect.");
                            Run();
                            break;
                        default:
                            Logger.Log(LogType.Info, ServiceName,
                                $"Reconnect for {clientToReconnect} skipped, client is already connected or not specified for specific reconnect.");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LogType.Info, ServiceName, "Reconnect delay or operation was canceled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, "Error during reconnect attempt execution.", ex);
                OnStatusChanged(false, $"Reconnect failed for {clientToReconnect ?? "service"}: {ex.Message}");
            }
        });
    }

    
}