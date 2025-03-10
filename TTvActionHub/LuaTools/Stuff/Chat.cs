using TwitchLib.Client;

namespace TTvActionHub.LuaTools.Stuff
{
    public static class Chat
    {
        public static TwitchClient? client;
        public static string? chat;

        public static void SendMessage(string message)
        {
            client?.SendMessage(chat, message);
        }

        public static async Task SendMessageAsync(string message)
        {
            await Task.Run(() => client?.SendMessage(chat, message));
        }
    }
}
