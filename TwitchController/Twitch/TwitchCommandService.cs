using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchController.Items;

namespace TwitchController.Twitch
{
    public class TwitchCommandService
    {
        private readonly ConnectionCredentials Credentials;
        private readonly Configuration _configuration;
        private readonly TwitchClient Client;

        public TwitchCommandService(Configuration config, string? onJoinMessage = null)
        {
            _configuration = config;

            Client = new TwitchClient();


            Credentials = new ConnectionCredentials(config.TwitchChannel, config.Token);

            Client.OnConnected += (sender, args) =>
            {
                Console.WriteLine($"[INFO] Commands service has connected to channel {_configuration.TwitchChannel}."); ;
            };

            if (_configuration.ShowLogs)
                Client.OnLog += (sender, args) => { Console.WriteLine($"[LOG] {args.Data}"); };

            if (onJoinMessage is string msg)
            {
                Client.OnJoinedChannel += (sender, args) =>
                {
                    Client.SendMessage(args.Channel, msg);
                };
            }
            Stuff.Chat.client = Client;
            Stuff.Chat.chat = config.TwitchChannel;
           
            Client.OnChatCommandReceived += OnChatCommandReceived;
        }

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs args)
        {
            //Getting command name from chat
            var cmd = args.Command.CommandText;
            var cmdArgs = args.Command.ArgumentsAsString;
            var cmdSender = args.Command.ChatMessage.Username;

            Console.WriteLine($"Received command: {cmd} from {cmdSender} with args: {cmdArgs}");

            if (!_configuration.Commands.TryGetValue(cmd, out Command? value)) return;

            if (_configuration.OpeningBracket is not null && _configuration.ClosingBracket is not null)
            {
                var start = cmdArgs.IndexOf(_configuration.OpeningBracket, StringComparison.Ordinal);
                var stop = cmdArgs.IndexOf(_configuration.ClosingBracket, StringComparison.Ordinal);
                if (start == -1 || stop == -1)
                    cmdArgs = "";
                else
                    cmdArgs = cmdArgs.Substring(start + 1, stop - start - 1);
            }

            value.Execute(cmdSender, cmdArgs.Split(' '));
        }

        public void Run()
        {
            Client.Initialize(Credentials, _configuration.TwitchChannel);
            Client.Connect();
        }
    }
}
