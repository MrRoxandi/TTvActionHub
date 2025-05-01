using TwitchLib.Communication.Events;
using System.Collections.Concurrent;
using TTvActionHub.LuaTools.Stuff;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TTvActionHub.Managers;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TwitchLib.Client;
using System.Net.NetworkInformation;
using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.Services
{
    public class TwitchChatService : IService, IUpdatableConfiguration
    {
        public TwitchClient? Client { get => _client; }
        public string Channel { get => _configuration.Login; }
        
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

        //private TaskCompletionSource<bool>? _commandCompletitionSource;
        private CancellationTokenSource? _serviceCancelationToken;
        private ConcurrentQueue<(TwitchCommand cmd, Users.USERLEVEL level, string sender, string[]? args)>? _commandsQueue;
        private Task? _workerTask;

        public TwitchChatService(IConfig config, LuaConfigManager manager)
        {
            _configManager = manager;
            _configuration = config;
            var commands = _configManager.LoadCommands() ?? throw new Exception($"Bad configuration for {ServiceName}");
            Commands = commands;
        }

        public void Run()
        {
            _stopRequested = false;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Starting service...");
            lock (_connectionLock)
            {
                try
                {
                    if (_client != null && _client.IsConnected)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Already running.");
                        OnStatusChanged(true);
                        return;
                    }
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing and connecting...");

                    _client = new TwitchClient();
                    _credentials = new ConnectionCredentials(_configuration.Login, _configuration.Token);

                    _client.OnChatCommandReceived += OnChatCommandReceived;
                    _client.OnConnectionError += OnConnectionErrorHandler;
                    _client.OnDisconnected += OnDisconnectedHandler;
                    _client.OnConnected += OnConnectedHandler;
                    _client.OnError += OnErrorHandler;
                    if (_configuration.LogState)
                        _client.OnLog += OnLogHandler;
                    _client.Initialize(_credentials, _configuration.Login);
                    _client.Connect();
                    
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing and starting worker...");
                    _serviceCancelationToken = new();
                    _commandsQueue = new();
                    //_commandCompletitionSource = new();
                    _workerTask = Task.Run(ProcessCommandQueueAsync, _serviceCancelationToken.Token);
                } 
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start.", ex);
                    OnStatusChanged(false, $"Startup failed: {ex.Message}");
                    _serviceCancelationToken?.Cancel();
                    CleanupClientResources();
                }
            }
        }

        private void OnLogHandler(object? sender, OnLogArgs e)
        {
             Logger.Log(LOGTYPE.INFO, ServiceName, e.Data);
        }

        public void Stop()
        {
            _stopRequested = true;
            _serviceCancelationToken?.Cancel();
            Logger.Log(LOGTYPE.INFO, ServiceName, "Disconnecting...");
            lock (_connectionLock)
            {

                try
                {
                    if (_client?.IsConnected ?? false)
                    {
                        _client.Disconnect();
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Already disconnected, ensuring cleanup on Stop request.");
                        CleanupClientResources();
                        OnStatusChanged(false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during disconnect request", ex);
                    OnStatusChanged(false, $"Shutdown error: {ex.Message}");
                    CleanupClientResources();
                }
            }
        }

        public bool UpdateConfiguration()
        {
            if (_configManager.LoadCommands() is not ConcurrentDictionary<string, TwitchCommand> cmds)
            {
                return false;
            }
            Commands = cmds;
            return true;
        }

        private void OnConnectedHandler(object? sender, OnConnectedArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Service has connected to channel {_configuration.Login}");
            OnStatusChanged(true);
        }

        private void OnDisconnectedHandler(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Service has disconnected");
            lock (_connectionLock)
            {
                if (_client != null)
                {
                    CleanupClientResources();
                }
            }
            OnStatusChanged(false, "Disconnected");
            HandleReconnect();
        }

        private void OnConnectionErrorHandler(object? sender, OnConnectionErrorArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName,$"Connection Error: {e.Error.Message}");
            lock (_connectionLock)
            {
                if (_client != null)
                {
                    CleanupClientResources();
                }
            }
            OnStatusChanged(false, $"Connection Error: {e.Error.Message}");
            HandleReconnect();
        }

        private void OnErrorHandler(object? sender, OnErrorEventArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName,$"Library Error: ", e.Exception);
        }

        private void HandleReconnect()
        {
            if (!_stopRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to reconnect in 5 seconds...");
                Task.Delay(5000).ContinueWith(_ =>
                {
                    if (!_stopRequested)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnecting...");
                        lock (_connectionLock)
                        {
                            try
                            {
                                if (_client != null && !_client.IsConnected)
                                {
                                    _client.Disconnect();
                                }
                                else if (_client == null)
                                {
                                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Cannot reconnect, client instance is null (unexpected). Stop might have been called.");
                                    OnStatusChanged(false, "Reconnect failed: client disposed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LOGTYPE.ERROR, ServiceName, "Reconnect failed.", ex);
                                OnStatusChanged(false, $"Reconnect failed: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnect cancelled, stop was requested during delay.");
                    }
                }, TaskScheduler.Default);
            }
        }

        protected virtual void OnStatusChanged(bool isRunning, string? message = null)
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

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            InsertCommandAsync(args.Command);
        }
        //Task.Run(() =>
        //{
        //    var cmd = args.Command.CommandText;
        //    var cmdArgStr = args.Command.ArgumentsAsString;
        //    var chatMessage = args.Command.ChatMessage;
        //    var cmdSender = chatMessage.Username;

        //    if (Commands == null) return;

        //    var result = Commands.TryGetValue(cmd, out TwitchCommand? value);
        //    if (!result || value == null) return;

        //    cmdArgStr = cmdArgStr.Replace("\U000e0000", "").Trim();
        //    Logger.Log(LOGTYPE.INFO, ServiceName, $"Received command: {cmd} from {cmdSender} with args: {cmdArgStr}");

        //    string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');

        //    value.Execute(
        //        cmdSender,
        //        Users.ParceFromTwitchLib(chatMessage.UserType, chatMessage.IsSubscriber, chatMessage.IsVip),
        //        cmdArgs);
        //});

        private void InsertCommandAsync(ChatCommand chatCommand)
        {
            var cmdName = chatCommand.CommandText;
            if (!(Commands?.TryGetValue(cmdName, out var command) ?? false))
                return;
            if (!command.CanExecute) return;
            var cmdArgStr = chatCommand.ArgumentsAsString.Replace("\U000e0000", "").Trim();
            var sender = chatCommand.ChatMessage.Username;
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Received command: {cmdName} from {sender} with args: {cmdArgStr}");
            var cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');
            _commandsQueue?.Enqueue(new(
                command, 
                Users.ParceFromTwitchLib(chatCommand.ChatMessage.UserType, chatCommand.ChatMessage.IsSubscriber, chatCommand.ChatMessage.IsVip), 
                sender, cmdArgs)
            );
        }

        private async Task ProcessCommandQueueAsync()
        {
            try
            {
                while (!_serviceCancelationToken!.IsCancellationRequested)
                {
                    if (_commandsQueue!.TryDequeue(out var RunData))
                    {
                        try
                        {
                            Logger.Log(LOGTYPE.INFO, ServiceName, $"Executing command from {RunData.sender}");
                            RunData.cmd.Execute(RunData.sender, RunData.level, RunData.args);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable execute command due to erorr", ex);
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), _serviceCancelationToken.Token);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Worker task was canceled");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during ProcessCommandQueue", ex);
            }
            finally
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Commands queue processing stopped");
            }
        }

        private void CleanupClientResources()
        {
            lock (_connectionLock)
            {
                if (_client == null) return;
                Logger.Log(LOGTYPE.INFO, ServiceName, "Cleaning up client resources...");
                try
                {
                    _workerTask?.GetAwaiter().GetResult();
                }
                catch (AggregateException ex)
                { 
                    foreach(var innerEx in ex.InnerExceptions)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception during command processing:", innerEx);
                    }
                }

                try
                {
                    _commandsQueue?.Clear();
                    _client.OnChatCommandReceived -= OnChatCommandReceived;
                    _client.OnConnectionError -= OnConnectionErrorHandler;
                    _client.OnDisconnected -= OnDisconnectedHandler;
                    _client.OnConnected -= OnConnectedHandler;
                    _client.OnError -= OnErrorHandler;
                    if (_configuration.LogState)
                        _client.OnLog -= OnLogHandler;
                    _client = null;
                    _credentials = null;
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Client resources cleaned up.");

                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception during client resource cleanup.", ex);
                }
            }
        }
    }
}
