using TwitchLib.Communication.Events;
using System.Collections.Concurrent;
using TTvActionHub.LuaTools.Stuff;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TTvActionHub.Managers; 
using TTvActionHub.Items; 
using TTvActionHub.Logs; 
using TwitchLib.Client;

namespace TTvActionHub.Services
{
    public class TwitchChatService : IService, IUpdatableConfiguration
    {
        private const int ReconnectDelaySeconds = 5;
        private const int WorkerQueuePollDelayMs = 100;
        private const int MaxConcurrentCommands = 5;
        private const int CommandShutdownWaitSeconds = 3;
        private const int WorkerTaskShutdownWaitSeconds = 3;

        public TwitchClient? Client { get => _client; }
        public string Channel => _configuration.Login; 

        public ConcurrentDictionary<string, TwitchCommand>? Commands { get; set; }
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public bool IsRunning => _client?.IsConnected ?? false; 
        public string ServiceName => "TwitchChat"; 

        private readonly LuaConfigManager _configManager;
        private readonly IConfig _configuration;
        private readonly object _connectionLock = new();

        private volatile bool _stopRequested = false;
        private ConnectionCredentials? _credentials;
        private TwitchClient? _client;

        private CancellationTokenSource? _serviceCts;
        private ConcurrentQueue<(TwitchCommand cmd, string cmdName, Users.USERLEVEL level, string sender, string[]? args)>? _commandsQueue;
        private Task? _workerTask;
        private SemaphoreSlim? _commandSemaphore;

        public TwitchChatService(IConfig config, LuaConfigManager manager)
        {
            _configManager = manager ?? throw new ArgumentNullException(nameof(manager));
            _configuration = config ?? throw new ArgumentNullException(nameof(config));

            Commands = _configManager.LoadCommands()
                ?? throw new InvalidOperationException($"Failed to load initial command configuration for {ServiceName}");
        }

        public void Run()
        {
            _stopRequested = false;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Starting service...");

            lock (_connectionLock)
            {
                if (IsRunning)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Service is already running.");
                    OnStatusChanged(true); 
                    return;
                }

                CleanupClientResources(); 

                try
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing and connecting...");

                    _serviceCts = new();
                    _commandsQueue = new();
                    _commandSemaphore = new SemaphoreSlim(MaxConcurrentCommands, MaxConcurrentCommands);

                    _client = new TwitchClient();
                    _credentials = new ConnectionCredentials(_configuration.Login, _configuration.Token);

                    SubscribeToClientEvents();

                    _client.Initialize(_credentials, _configuration.Login);
                    _client.Connect(); 

                    _workerTask = Task.Run(() => ProcessCommandQueueAsync(_serviceCts.Token), _serviceCts.Token);

                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start service.", ex);
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
            Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to update configuration...");
            try
            {
                if (_configManager.LoadCommands() is ConcurrentDictionary<string, TwitchCommand> cmds)
                {
                    Commands = cmds; 
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Configuration updated successfully.");
                    return true;
                }
                else
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Failed to update configuration: LoadCommands returned null.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to update configuration due to an error.", ex);
                return false;
            }
        }

        private void SubscribeToClientEvents()
        {
            if (_client == null) return;
            _client.OnChatCommandReceived += OnChatCommandReceived;
            _client.OnConnectionError += OnConnectionErrorHandler;
            _client.OnDisconnected += OnDisconnectedHandler;
            _client.OnConnected += OnConnectedHandler;
            _client.OnError += OnErrorHandler;
            if (_configuration.LogState)
                _client.OnLog += OnLogHandler;
        }

        private void UnsubscribeFromClientEvents()
        {
            if (_client == null) return;
            _client.OnChatCommandReceived -= OnChatCommandReceived;
            _client.OnConnectionError -= OnConnectionErrorHandler;
            _client.OnDisconnected -= OnDisconnectedHandler;
            _client.OnConnected -= OnConnectedHandler;
            _client.OnError -= OnErrorHandler;
            if (_configuration.LogState) 
                _client.OnLog -= OnLogHandler;
        }

        private void OnConnectedHandler(object? sender, OnConnectedArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Successfully connected to channel '{e.AutoJoinChannel}'");
            OnStatusChanged(true, "Connected");
        }

        private void OnDisconnectedHandler(object? sender, OnDisconnectedEventArgs e)
        {
            Logger.Log(LOGTYPE.WARNING, ServiceName, "Disconnected from Twitch.");
            HandleDisconnection("Disconnected");
        }

        private void OnConnectionErrorHandler(object? sender, OnConnectionErrorArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, $"Connection error: {e.Error.Message}");
            HandleDisconnection($"Connection Error: {e.Error.Message}");
        }

        private void OnErrorHandler(object? sender, OnErrorEventArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName, "TwitchLib internal error.", e.Exception);
        }

        private void OnLogHandler(object? sender, OnLogArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, e.Data);
        }

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            EnqueueCommand(args.Command);
        }

        private void EnqueueCommand(ChatCommand chatCommand)
        {
            if (_commandsQueue == null || Commands == null || _serviceCts?.IsCancellationRequested == true)
            {
                return;
            }

            var cmdName = chatCommand.CommandText;
            if (!Commands.TryGetValue(cmdName, out var command) || command == null) 
            {
                return; 
            }

            if (!command.CanExecute)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Command '{cmdName}' cannot be executed right now.");
                return; 
            }

            var cmdArgStr = chatCommand.ArgumentsAsString.Replace("\U000e0000", "").Trim();
            var sender = chatCommand.ChatMessage.Username;
            var userLevel = Users.ParceFromTwitchLib(
                chatCommand.ChatMessage.UserType,
                chatCommand.ChatMessage.IsSubscriber,
                chatCommand.ChatMessage.IsVip);
            string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');

            Logger.Log(LOGTYPE.INFO, ServiceName, $"Queueing command: '{cmdName}' from {sender} with args: [{cmdArgStr}]");

            _commandsQueue.Enqueue((command, cmdName, userLevel, sender, cmdArgs));
        }

        private async Task ProcessCommandQueueAsync(CancellationToken cancellationToken)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Command processing worker started.");
            var runningTasks = new List<Task>(); 

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    runningTasks.RemoveAll(t => t.IsCompleted);

                    if (_commandsQueue!.TryDequeue(out var runData))
                    {
                        if (_commandSemaphore!.CurrentCount == 0)
                        {
                            await Task.Delay(WorkerQueuePollDelayMs, cancellationToken);
                            _commandsQueue.Enqueue(runData); 
                            continue;
                        }

                        try
                        {
                            await _commandSemaphore.WaitAsync(cancellationToken);

                            Task commandTask = Task.Run(() => 
                            {
                                try
                                {
                                    string commandIdentifier = $"'{runData.cmdName}' from {runData.sender}"; 
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Executing command {commandIdentifier}...");
                                    runData.cmd.Execute(runData.sender, runData.level, runData.args); 
                                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Finished command {commandIdentifier}.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error executing command '{runData.cmdName}' from {runData.sender}", ex);
                                }
                                finally
                                {
                                    _commandSemaphore.Release(); 
                                }
                            }, cancellationToken); 

                            runningTasks.Add(commandTask); 
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Operation canceled while waiting for semaphore or starting command task.");
                            break; 
                        }
                        catch (Exception ex) 
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error dispatching command '{runData.cmdName}': {ex.Message}", ex);
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
                Logger.Log(LOGTYPE.INFO, ServiceName, "Command processing loop was canceled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unhandled error in command processing loop", ex);

            }
            finally
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Worker loop finished. Waiting for {runningTasks.Count} running command(s) to complete...");
                try
                {
                    await Task.WhenAll(runningTasks).WaitAsync(TimeSpan.FromSeconds(CommandShutdownWaitSeconds));
                    Logger.Log(LOGTYPE.INFO, ServiceName, "All tracked commands finished or timed out.");
                }
                catch (TimeoutException)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Timeout waiting for running commands to complete upon shutdown.");
                }
                catch (Exception ex) 
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"Exception while waiting for running commands: {ex.Message}");
                }
                Logger.Log(LOGTYPE.INFO, ServiceName, "Command processing worker stopped.");
            }
        }

        // --- Вспомогательные методы ---
        private void HandleDisconnection(string reason)
        {
            lock (_connectionLock)
            {
                CleanupClientResources();
            }
            OnStatusChanged(false, reason); // Сообщаем о дисконнекте

            AttemptReconnect();
        }

        private void AttemptReconnect()
        {
            if (!_stopRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Attempting to reconnect in {ReconnectDelaySeconds} seconds...");
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds), _serviceCts?.Token ?? CancellationToken.None); // Используем токен, если он есть

                        if (!_stopRequested) 
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnecting...");
                            Run();
                        }
                        else
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect cancelled, service stop was requested during delay.");
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
                _serviceCts.Cancel();
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
                catch (OperationCanceledException)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Worker task wait was canceled (expected).");
                }
                catch (AggregateException ae)
                {
                    ae.Handle(ex =>
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception occurred in worker task during shutdown.", ex);
                        return true; 
                    });
                }
            }
            _workerTask = null; 
        }

        private void DisconnectClient()
        {
            if (_client?.IsConnected ?? false)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Disconnecting Twitch client...");
                try
                {
                    _client.Disconnect();
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during client disconnect request.", ex);
                }
            }
        }

        private void CleanupClientResources()
        {
            if (_client != null)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up Twitch client resources...");
                UnsubscribeFromClientEvents();
                _client = null; 
                _credentials = null;
                Logger.Log(LOGTYPE.INFO, ServiceName, "Twitch client resources cleaned up.");
            }
        }

        private void CleanupNonClientResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up non-client resources (CTS, Semaphore)...");
            _serviceCts?.Cancel(); 
            _serviceCts?.Dispose();
            _serviceCts = null;

            _commandSemaphore?.Dispose();
            _commandSemaphore = null;

            _commandsQueue?.Clear();
            _commandsQueue = null;

            Logger.Log(LOGTYPE.INFO, ServiceName, "Non-client resources cleaned up.");
        }

        private void CleanupResources()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Performing full resource cleanup...");
            StopInternal(); 
            lock (_connectionLock)
            {
                DisconnectClient();
                CleanupClientResources();
            }
            CleanupNonClientResources();
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