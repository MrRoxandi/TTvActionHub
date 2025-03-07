using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TTvActionHub.Items;
using TTvActionHub.LuaTools.Stuff;
using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public class CommandsService : IService
    {
        private readonly ConnectionCredentials _credentials;
        private readonly Configuration _configuration;
        private readonly TwitchClient _client;

        public CommandsService(Configuration config, string? onJoinMessage = null)
        {
            _configuration = config;
            _client = new TwitchClient();
            _credentials = new ConnectionCredentials(config.TwitchInfo.Login, config.TwitchInfo.Token);

            _client.OnDisconnected += (sender, args) =>
            {
                Logger.Log(LOGTYPE.INFO, ServiceName(), $"Service has disconnected");
            };

            _client.OnConnected += (sender, args) =>
            {
                Logger.Log(LOGTYPE.INFO, ServiceName(), $"Service has connected to channel {_configuration.TwitchInfo.Login}"); ;
            };

            if (_configuration.ShowLogs)
                _client.OnLog += (sender, args) => {
                    Logger.Log(LOGTYPE.INFO, ServiceName(), args.Data);
                };

            if (onJoinMessage is string msg)
            {
                _client.OnJoinedChannel += (sender, args) =>
                {
                    _client.SendMessage(args.Channel, msg);
                };
            }

            Chat.client = _client;
            Chat.chat = config.TwitchInfo.Login;

            _client.OnChatCommandReceived += OnChatCommandReceived;
        }

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            //Getting command name from chat
            var cmd = args.Command.CommandText;
            var cmdArgStr = args.Command.ArgumentsAsString;
            var chatMessage = args.Command.ChatMessage;
            var cmdSender = chatMessage.Username;
            
            if (!_configuration.Commands.TryGetValue(cmd, out Command? value)) return;

            if (!string.IsNullOrEmpty(_configuration.OpeningBracket) && !string.IsNullOrEmpty(_configuration.ClosingBracket))
            {
                var start = cmdArgStr.IndexOf(_configuration.OpeningBracket, StringComparison.Ordinal);
                var stop = cmdArgStr.IndexOf(_configuration.ClosingBracket, StringComparison.Ordinal);
                if (start == -1 || stop == -1)
                    cmdArgStr = "";
                else
                    cmdArgStr = cmdArgStr.Substring(start + 1, stop - start - 1);
            }
            cmdArgStr = cmdArgStr.Replace("\U000e0000", "").Trim();
            Logger.Log(LOGTYPE.INFO, ServiceName(), $"Received command: {cmd} from {cmdSender} with args: {cmdArgStr}");

            string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');
            
            value.Execute(
                cmdSender, 
                Users.ParceFromTwitchLib(chatMessage.UserType, chatMessage.IsSubscriber, chatMessage.IsVip),
                cmdArgs);
        }

        public void Run()
        {
            _client.Initialize(_credentials, _configuration.TwitchInfo.Login);
            _client.Connect();
        }

        public void Stop() {
            _client.Disconnect();
        }

        public string ServiceName() => "CommandService";
    }
}
