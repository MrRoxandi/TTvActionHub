using TTvActionHub.Services;
using TwitchLib.Client.Enums;

namespace TTvActionHub.LuaTools.Services
{
    public static class TwitchTools
    {

        internal static TwitchService? Service { get; set; }

        public static void SendMessage(string message)
        {
            if (Service is not { } client)
                throw new Exception("Unable to send twitch chat message. Client is null");
            client.SendMessage(message);
        }

        public static void SendWhisper(string target, string message)
        {
            if (Service is not { } client)
                throw new Exception("Unable to send twitch chat message. Client is null");
            client.SendWhisper(target, message);
        }

        public static void AddPoints(string name, int value)
        {
            if (Service is not { } client)
            {
                throw new Exception("Unable to send twitch chat message. Client is null");
            }
            client.AddPointsToUserAsync(name, value).GetAwaiter().GetResult();
        }

        public static long GetPoints(string name)
        {
            if (Service is not { })
            {
                throw new Exception("Unable to send twitch chat message. Client is null");
            }
            return TwitchService.GetPointsFromUser(name).GetAwaiter().GetResult();
        }

        public static long GetEventCost(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) throw new Exception($"Unable to get cost for event with empty name");
            if (Service is not { } client)
            {
                throw new Exception("Unable to send twitch chat message. Client is null");
            }
            var cost = client.GetEventCost(eventName);
            if (cost is not { } c)
            {
                throw new Exception($"Unable to get cost of {eventName} event. Reason: this events doesn't exist.");
            }
            return c;
        }

        public enum PermissionLevel : byte
        {
            Viewer, Vip, Subscriber, Moderator, Broadcaster
        }

        public enum TwitchEventKind : byte
        {
            Command = 0, TwitchReward, /*PointsReward*/
        }

        public static PermissionLevel ParseFromTwitchLib(UserType type, bool isSub, bool isVip)
        {
            return type switch
            {
                UserType.Moderator or UserType.GlobalModerator or UserType.Staff or UserType.Admin => PermissionLevel.Moderator,
                UserType.Broadcaster => PermissionLevel.Broadcaster,
                _ => isSub ? PermissionLevel.Subscriber : isVip ? PermissionLevel.Vip : PermissionLevel.Viewer,
            };
        }
    }
}
