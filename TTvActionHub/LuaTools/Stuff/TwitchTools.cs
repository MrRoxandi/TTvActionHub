using TTvActionHub.Services;
using TwitchLib.Client.Enums;

namespace TTvActionHub.LuaTools.Stuff
{
    public static class TwitchTools
    {

        public static TwitchService? Service { get; set; }

        public static void SendMessage(string message)
        {
            if (Service is not TwitchService client)
                throw new Exception("Unable to send twitch chat message. Client is null");
            client.SendMessage(message);
        }

        public static void SendWhisper(string target, string message)
        {
            if (Service is not TwitchService client)
                throw new Exception("Unable to send twitch chat message. Client is null");
            client.SendWhisper(target, message);
        }

        public enum PermissionLevel : int
        {
            VIEWIER, VIP, SUB, MODERATOR, BROADCASTER
        }

        public enum TwitchEventKind : byte
        {
            Command = 0, TwitchReward, PointsReward
        }

        public static PermissionLevel ParceFromTwitchLib(UserType type, bool issub, bool isvip)
        {
            return type switch
            {
                UserType.Moderator or UserType.GlobalModerator or UserType.Staff or UserType.Admin => PermissionLevel.MODERATOR,
                UserType.Broadcaster => PermissionLevel.BROADCASTER,
                _ => issub ? PermissionLevel.SUB : isvip ? PermissionLevel.VIP : PermissionLevel.VIEWIER,
            };
        }
    }
}
