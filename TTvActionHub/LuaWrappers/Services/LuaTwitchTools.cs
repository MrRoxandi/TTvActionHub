using Lua;
using TTvActionHub.BackEnds.Abstractions;

namespace TTvActionHub.LuaWrappers.Services;

[LuaObject]
public partial class LuaTwitchTools
{
    [LuaMember]
    public static void SendMessage(string message) => TwitchTools.SendMessage(message);
    
    [LuaMember]
    public static void SendWhisper(string message) => TwitchTools.SendMessage(message);
    
    [LuaMember]
    public static void AddPoints(string name, int value) => TwitchTools.AddPoints(name, value);
    
    [LuaMember]
    public static long GetPoints(string name) => TwitchTools.GetPoints(name);
    
    [LuaMember]
    public static void SetPoints(string name, int value) => TwitchTools.SetPoints(name, value);
    
    [LuaMember]
    public static long GetEventCost(string name) => TwitchTools.GetEventCost(name);

    [LuaMember]
    public static int PermissionLevel(string name) => name switch
        {
            "Vip" => (int)TwitchTools.PermissionLevel.Vip,
            "Subscriber" => (int)TwitchTools.PermissionLevel.Subscriber,
            "Moderator" => (int)TwitchTools.PermissionLevel.Moderator,
            "Broadcaster" => (int)TwitchTools.PermissionLevel.Broadcaster,
            _ => (int)TwitchTools.PermissionLevel.Viewer
        };

    [LuaMember] public static int TwitchEventKind(string kind) => kind switch
        {
            "Command" => (int)TwitchTools.TwitchEventKind.Command,
            "Reward" => (int)TwitchTools.TwitchEventKind.TwitchReward,
            _ => throw new NotImplementedException()
        };
    
}