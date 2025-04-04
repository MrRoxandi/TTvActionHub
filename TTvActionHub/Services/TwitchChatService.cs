using TwitchLib.Communication.Events;
using TTvActionHub.LuaTools.Stuff;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TwitchLib.Client;

namespace TTvActionHub.Services
{
    public class TwitchChatService(IConfig config) : IService
    {
        public TwitchClient? Client { get => _client; }
        public string Channel { get => _configuration.TwitchInfo.Login; }

        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public string ServiceName => "TwitchChat";
        public bool IsRunning => _client?.IsConnected ?? false;
        private volatile bool _stopRequested = false;

        private ConnectionCredentials? _credentials;
        private readonly IConfig _configuration = config;
        private TwitchClient? _client;  

        public void Run()
        {
            _stopRequested = false;
            if (_client == null)
            {
                _client = new TwitchClient();
                _credentials = new ConnectionCredentials(_configuration.TwitchInfo.Login, _configuration.TwitchInfo.Token);

                _client.OnChatCommandReceived += OnChatCommandReceived;
                _client.OnConnectionError += OnConnectionErrorHandler;
                _client.OnDisconnected += OnDisconnectedHandler;
                _client.OnConnected += OnConnectedHandler;
                _client.OnError += OnErrorHandler;
                if (_configuration.LogState) _client.OnLog += (sender, args) => Logger.Log(LOGTYPE.INFO, ServiceName, args.Data);
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing and connecting...");
            try
            {
                if (IsRunning)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Already running.");
                    OnStatusChanged(true);
                    return;
                }
                _client.Initialize(_credentials, _configuration.TwitchInfo.Login);
                _client.Connect();
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start.", ex);
                OnStatusChanged(false, $"Startup failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            _stopRequested = true;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Disconnecting...");
            try
            {
                if(_client != null)
                {
                    if (_client.IsConnected)
                    {
                        _client.Disconnect();

                        _client.OnChatCommandReceived -= OnChatCommandReceived;
                        _client.OnConnectionError -= OnConnectionErrorHandler;
                        _client.OnDisconnected -= OnDisconnectedHandler;
                        _client.OnConnected -= OnConnectedHandler;
                        _client.OnError -= OnErrorHandler;
                        if (_configuration.LogState) _client.OnLog -= (a, b) => { };
                        _client = null;
                        _credentials = null;
                    }
                    else
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, "Already disconnected.");
                        OnStatusChanged(false);
                    }
                }
                
                
            } catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error during disconect", ex);
                OnStatusChanged(false, $"Shutdown error: {ex.Message}");
            }
        }

        private void OnConnectedHandler(object? sender, OnConnectedArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Service has connected to channel {_configuration.TwitchInfo.Login}");
            OnStatusChanged(true);
        }

        private void OnDisconnectedHandler(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Service has disconnected");
            OnStatusChanged(false, "Disconnected");
            HandleReconnect();
        }

        private void OnConnectionErrorHandler(object? sender, OnConnectionErrorArgs e)
        {
            Logger.Log(LOGTYPE.ERROR, ServiceName,$"Connection Error: {e.Error.Message}");
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
                        try
                        {
                            if (!_client!.IsConnected)
                            {
                                _client.Connect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, "Reconnect failed.", ex);
                            OnStatusChanged(false, $"Reconnect failed: {ex.Message}");
                        }
                    }
                });
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

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args) => Task.Run(() =>
        {
            var cmd = args.Command.CommandText;
            var cmdArgStr = args.Command.ArgumentsAsString;
            var chatMessage = args.Command.ChatMessage;
            var cmdSender = chatMessage.Username;

            if (!_configuration.Commands.TryGetValue(cmd, out Command? value)) return;
            var (obr, cbr) = _configuration.Brackets;

            if (!string.IsNullOrEmpty(obr) && !string.IsNullOrEmpty(cbr))
            {
                var start = cmdArgStr.IndexOf(obr, StringComparison.Ordinal);
                var stop = cmdArgStr.IndexOf(cbr, StringComparison.Ordinal);
                if (start == -1 || stop == -1)
                    cmdArgStr = "";
                else
                    cmdArgStr = cmdArgStr.Substring(start + 1, stop - start - 1);
            }
            cmdArgStr = cmdArgStr.Replace("\U000e0000", "").Trim();
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Received command: {cmd} from {cmdSender} with args: {cmdArgStr}");

            string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');

            value.Execute(
                cmdSender,
                Users.ParceFromTwitchLib(chatMessage.UserType, chatMessage.IsSubscriber, chatMessage.IsVip),
                cmdArgs);
        });

    }
}
