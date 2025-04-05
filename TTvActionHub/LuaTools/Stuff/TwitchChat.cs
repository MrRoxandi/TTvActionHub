using TwitchLib.Client;

namespace TTvActionHub.LuaTools.Stuff
{
    public static class TwitchChat
    {
        public static TwitchClient? Client { get; set; }
        public static string? Channel { get; set; }

        public static void SendMessage(string message)
        {
            if (Client is not TwitchClient client)
                throw new Exception("Unable to send twitch chat message. Client is null");
            if (string.IsNullOrEmpty(Channel))
                throw new Exception("Unable to send twitch chat message. Channel is empty");
            client.SendMessage(Channel, message);
        }
    }
}
