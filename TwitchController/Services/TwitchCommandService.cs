using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchController.Items;
using TwitchController.LuaTools.Stuff;
using TwitchController.Logs;

namespace TwitchController.Services
{
    internal class TwitchCommandService : IService
    {
        private readonly ConnectionCredentials Credentials;
        private readonly Configuration _configuration;
        private readonly TwitchClient Client;

        public TwitchCommandService(Configuration config, string? onJoinMessage = null)
        {
            _configuration = config;

            Client = new TwitchClient();


            Credentials = new ConnectionCredentials(config.TwitchInfo.Login, config.TwitchInfo.Token);

            Client.OnConnected += (sender, args) =>
            {
                Logger.Info($"Commands service has connected to channel {_configuration.TwitchInfo.Login}."); ;
            };

            if (_configuration.ShowLogs)
                Client.OnLog += (sender, args) => {
                    Logger.External(LOGTYPE.INFO, "TwitchCommandService", args.Data);
                };

            if (onJoinMessage is string msg)
            {
                Client.OnJoinedChannel += (sender, args) =>
                {
                    Client.SendMessage(args.Channel, msg);
                };
            }
            Chat.client = Client;
            Chat.chat = config.TwitchInfo.Login;

            Client.OnChatCommandReceived += OnChatCommandReceived;
        }

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            //Getting command name from chat
            var cmd = args.Command.CommandText;
            var cmdArgStr = args.Command.ArgumentsAsString;
            var cmdSender = args.Command.ChatMessage.Username;
            
            if (!_configuration.Commands.TryGetValue(cmd, out Command? value)) return;

            Logger.Info($"Received command: {cmd} from {cmdSender} with args: {cmdArgStr}");

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
            string[]? cmdArgs = string.IsNullOrEmpty(cmdArgStr) ? null : cmdArgStr.Split(' ');
            value.Execute(cmdSender, cmdArgs);
        }

        public void Run()
        {
            Client.Initialize(Credentials, _configuration.TwitchInfo.Login);
            Client.Connect();
        }

        public void Stop() {
            Client.Disconnect();
        }
    }
}
