using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client;

namespace TwitchController.Stuff
{
    public static class Chat
    {
        public static TwitchClient client;
        public static string chat;

        public static void SendMessage(string message)
        {
            client.SendMessage(chat, message);
        }

        public static async Task SendMessageAsync(string message)
        {
            await Task.Run(() => client.SendMessage(chat, message));
        }
    }
}
