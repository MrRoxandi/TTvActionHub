using System.Collections.Concurrent;
using System.Text;
using NLua;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services;

namespace TTvActionHub.Managers;

public class LuaConfigManager
{
    // --- Public block ---

    private static string ConfigsPath { get; } =
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs")).FullName;

    private static string ServiceName => "LuaConfigManager";
    private static IEnumerable<string> ConfigNames { get; } = ["config.lua", "twitchevents.lua", "timeractions.lua"];

    public bool ForceRelog { get; }

    public bool MoreLogs { get; }

    private readonly long _stdTimeOut;

    public LuaConfigManager()
    {
        _lua = new Lua();
        _lua.LoadCLRPackage();
        _lua.State.Encoding = Encoding.UTF8;
        const string fileName = "config.lua";
        var fileResult = ParseLuaFile(fileName);
        if (fileResult is null) throw new Exception($"File {fileName} is not a proper config. Check syntax.");
        if (fileResult["force-reload"] is not bool forceRelog)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['force-relog'] is not presented. Will be used default value: [{false}]");
            ForceRelog = false;
        }
        else
        {
            ForceRelog = forceRelog;
        }

        if (fileResult["logs"] is not bool moreLogs)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['logs'] is not presented. Will be used default value: [{false}]");
            MoreLogs = false;
        }
        else
        {
            MoreLogs = moreLogs;
        }

        if (fileResult["timeout"] is not long stdTimeOut)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['timeout'] is not presented. Will be used default value: [{30000}] ms");
            _stdTimeOut = 30000;
        }
        else if (stdTimeOut < 0)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['timeout'] have [{stdTimeOut}] value. But valid value must be > 0. Will be used default value: [{30000}] ms");
            _stdTimeOut = 30000;
        }
        else
        {
            _stdTimeOut = stdTimeOut;
        }

        Logger.Log(LogType.Info, ServiceName, "Configuration loaded successfully");
    }

    public ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>? LoadTwitchEvents()
    {
        const string fileName = "twitchevents.lua";
        var fileResult = ParseLuaFile(fileName);
        if (fileResult is null) return null;

        if (fileResult.Keys.Count == 0)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
            return [];
        }

        var result = new ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>();
        foreach (var key in fileResult.Keys)
        {
            if (fileResult[key] is not LuaTable twEventTable)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{key}'] is not a TwitchEvent. Check syntax. Aborting loading process ...");
                return null;
            }

            if (twEventTable["kind"] is not TwitchTools.TwitchEventKind kind)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{key}']['kind'] is not a TwitchEventKind. Check syntax. Aborting loading process ...");
                return null;
            }

            if (twEventTable["action"] is not LuaFunction action)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{key}']['action'] is not an action. Check syntax. Aborting loading process ...");
                return null;
            }

            long? timeOut = null;
            long cmdCost = 0;
            var perm = TwitchTools.PermissionLevel.Viewer;
            if (kind != TwitchTools.TwitchEventKind.TwitchReward)
            {
                if (twEventTable["timeout"] is not long time)
                {
                    Logger.Log(LogType.Warning, ServiceName,
                        $"In file {fileName} ['{key}']['timeout'] is not a timeout. Will be used default value: {_stdTimeOut} ms");
                    time = _stdTimeOut;
                }

                timeOut = time;

                if (twEventTable["perm"] is not TwitchTools.PermissionLevel userLevel)
                {
                    Logger.Log(LogType.Warning, ServiceName,
                        $"In file {fileName} ['{key}']['perm'] is not a permission level. Will be used default value: Viewer");
                    userLevel = TwitchTools.PermissionLevel.Viewer;
                }

                if (twEventTable["cost"] is not long cost) cost = 0;
                cmdCost = cost;
                perm = userLevel;
            }

            result.TryAdd((key.ToString()!, kind),
                new TwitchEvent(kind, action, key.ToString()!, perm, timeOut, cmdCost));
        }

        return result;
    }

    public ConcurrentDictionary<string, TimerAction>? LoadTActions()
    {
        const string fileName = "timeractions.lua";
        var fileResult = ParseLuaFile(fileName);
        if (fileResult is null) return null;
        if (fileResult.Keys.Count == 0)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
            return [];
        }

        var actions = new ConcurrentDictionary<string, TimerAction>();
        foreach (var keyTAction in fileResult.Keys)
        {
            if (fileResult[keyTAction] is not LuaTable tActionTable)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} [{keyTAction}] is not a TimerAction. Check syntax. Aborting loading process ...");
                return null;
            }

            if (tActionTable["action"] is not LuaFunction action)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} [{keyTAction}]['action'] is not an action. Check syntax. Aborting loading process...");
                return null;
            }

            if (tActionTable["timeout"] is not long timeOut)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} [{keyTAction}]['timeout'] is not valid value. Check syntax. Aborting loading process...");
                return null;
            }

            if (timeOut <= 0)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} [{keyTAction}]['timeout'] is not in valid range. Allowed range [1, inf). Aborting loading process...");
                return null;
            }

            if (!actions.TryAdd(keyTAction.ToString()!,
                    new TimerAction { Function = action, Name = keyTAction.ToString()!, TimeOut = timeOut }))
            {
                Logger.Log(LogType.Warning, ServiceName,
                    $"For some reason [{keyTAction}] wasn't added to collection. Aborting loading process...");
                return null;
            }
        }

        return actions;
    }

    // --- Static checker for configs ---

    public static bool CheckConfiguration()
    {
        return ConfigNames.All(config => File.Exists(Path.Combine(ConfigsPath, config)));
    }

    // --- Private block ---

    private LuaTable? ParseLuaFile(string configName)
    {
        var fullPath = Path.Combine(ConfigsPath, configName);
        try
        {
            var result = _lua.DoFile(fullPath);
            var state = result[0];
            if (state is not LuaTable table)
                throw new Exception($"Returned result form {configName} was not a valid table. Check syntax");
            return table;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "During parsing lua file occured en error:", ex);
            return null;
        }
    }

    // All available static bridges for lua
    private static List<string> Bridges =>
    [
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Keyboard",
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Mouse",
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Services\").Audio",
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Services\").Container",
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Services\").TwitchTools",
        "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Funcs"
    ];

    private static void GenerateTwitchEventsFile()
    {
        var filePath = Path.Combine(ConfigsPath, "twitchevents.lua");
        if (File.Exists(filePath)) return;
        StringBuilder builder = new();
        Bridges.ForEach(bridge => builder.AppendLine($"local {bridge.Split(").").Last()} = import {bridge}"));
        builder.AppendLine();
        builder.AppendLine("local twitchevents = {}");
        builder.AppendLine();
        builder.AppendLine("twitchevents['ping'] = {}");
        builder.AppendLine("twitchevents['ping']['kind'] = TwitchTools.TwitchEventKind.Command");
        builder.AppendLine("twitchevents['ping']['action'] = ");
        builder.AppendLine("\tfunction(sender, args)");
        builder.AppendLine("\t\tTwitchTools.SendMessage('@'..sender..' -> pong')");
        builder.AppendLine("\tend");
        builder.AppendLine("twitchevents['ping']['timeout'] = 1000 -- 1000 ms");
        builder.AppendLine("twitchevents['ping']['perm'] = TwitchTools.PermissionLevel.Viewer");
        builder.AppendLine();
        builder.AppendLine("twitchevents['test'] = {}");
        builder.AppendLine("twitchevents['test']['kind'] = TwitchTools.TwitchEventKind.TwitchReward");
        builder.AppendLine("twitchevents['test']['action'] = ");
        builder.AppendLine("\tfunction(sender, args)");
        builder.AppendLine("\t\tTwitchTools.SendMessage('@'..sender..' -> test')");
        builder.AppendLine("\tend");
        builder.AppendLine("return twitchevents");
        File.WriteAllText(filePath, builder.ToString());
    }

    private static void GenerateTimerActionsFile()
    {
        var filePath = Path.Combine(ConfigsPath, "timeractions.lua");
        if (File.Exists(filePath)) return;
        StringBuilder builder = new();
        Bridges.ForEach(bridge => builder.AppendLine($"local {bridge.Split(").").Last()} = import {bridge}"));
        builder.AppendLine();
        builder.AppendLine("local timeractions = {}");
        builder.AppendLine();
        builder.AppendLine("--timeractions['test'] = {}");
        builder.AppendLine("--timeractions['test']['action'] =");
        builder.AppendLine("\t--function()");
        builder.AppendLine("\t\t--TwitchChat.SendMessage('Just a test -> test')");
        builder.AppendLine("\t--end");
        builder.AppendLine("--timeractions['test']['timeout'] = 10000 -- 10000 ms");
        builder.AppendLine();
        builder.AppendLine("return timeractions");
        File.WriteAllText(filePath, builder.ToString());
    }

    private static void GenerateMainConfig()
    {
        var filePath = Path.Combine(ConfigsPath, "config.lua");
        if (File.Exists(filePath)) return;
        StringBuilder builder = new();
        builder.AppendLine();
        builder.AppendLine("local configuration = {}");
        builder.AppendLine();
        builder.AppendLine("configuration['force-relog'] = false");
        builder.AppendLine("configuration['timeout'] = 30000 -- 30000 ms == 30 s");
        builder.AppendLine("configuration['logs'] = false");
        builder.AppendLine();
        builder.AppendLine("return configuration");
        File.WriteAllText(filePath, builder.ToString());
    }

    public static void GenerateAllConfigs()
    {
        GenerateTwitchEventsFile();
        GenerateTimerActionsFile();
        GenerateMainConfig();
    }

    // --- Private fields ---
    private readonly Lua _lua;
}