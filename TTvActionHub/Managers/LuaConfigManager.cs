using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Lua;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services;
using TTvActionHub.LuaTools.Wrappers.Hardware;
using TTvActionHub.LuaTools.Wrappers.Services;
using TTvActionHub.LuaTools.Wrappers.Stuff;

namespace TTvActionHub.Managers;

public class LuaConfigManager
{
    // --- Public block ---

    private static string ConfigsPath { get; } =
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs")).FullName;

    private static string ServiceName => "LuaConfigManager";
    private static IEnumerable<string> ConfigNames { get; } = ["Config.lua", "TwitchEvents.lua", "TimerActions.lua"];

    public bool ForceRelog { get; private set; }
    public bool MoreLogs { get; private set; }

    private long _stdTimeOut;

    public LuaConfigManager()
    {
        // --- Setuping lua engine ---
        const string fileName = "Config.lua";
        _lua = LuaState.Create();
        _lua.Environment["Funcs"] = new LuaFuncs();
        _lua.Environment["Audio"] = new LuaAudio();
        _lua.Environment["Container"] = new LuaContainer();
        _lua.Environment["TwitchTools"] = new LuaTwitchTools();
        _lua.Environment["Keyboard"] = new LuaKeyboard();
        _lua.Environment["Mouse"] = new LuaMouse();
        // --- Reading config ---
        var fileResult = ParseLuaFile(fileName).GetAwaiter().GetResult() ??
                         throw new Exception($"File {fileName} is not a proper config. Check syntax.");
        
        // --- Trying to get configs --- 
        if (fileResult["force-relog"].Type != LuaValueType.Boolean)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['force-relog'] is not presented. Will be used default value: [{false}]");
            ForceRelog = false;
        }
        else
        {
            ForceRelog = fileResult["force-relog"].Read<bool>();
        }

        if (fileResult["logs"].Type != LuaValueType.Boolean)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['logs'] is not presented. Will be used default value: [{false}]");
            MoreLogs = false;
        }
        else
        {
            MoreLogs = fileResult["logs"].Read<bool>();
            ;
        }

        if (fileResult["timeout"].Type != LuaValueType.Number)
        {
            Logger.Log(LogType.Warning, ServiceName,
                $"In file {fileName} ['timeout'] is not presented. Will be used default value: [{30000}] ms");
            _stdTimeOut = 30000;
        }
        else
        {
            var timeout = fileResult["timeout"].Read<int>();
            if (timeout < 0)
            {
                Logger.Log(LogType.Warning, ServiceName,
                    $"In file {fileName} ['timeout'] have [{_stdTimeOut}] value. But valid value must be > 0. Will be used default value: [{30000}] ms");
                _stdTimeOut = 30000;
            }
            else
            {
                _stdTimeOut = timeout;
            }
        }

        Logger.Log(LogType.Info, ServiceName, "Main configuration loaded successfully");
    }

    public ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>? LoadTwitchEvents()
    {
        var fileName = "TwitchEvents.lua";
        var configTable = ParseLuaFile(fileName).GetAwaiter().GetResult() ??
                         throw new Exception($"File {fileName} is not a proper config. Check syntax.");

        if (configTable.HashMapCount == 0)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
            return [];
        }

        var result = new ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>();
        var previosKey = LuaValue.Nil;
        while (configTable.TryGetNext(previosKey, out var kvp))
        {
            var currentKey = kvp.Key;
            if (kvp.Value.Type != LuaValueType.Table)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}'] is not a TwitchEvent. Check syntax. Aborting loading process ...");
                return null;
            }

            var twEventTable = kvp.Value.Read<LuaTable>();
            if (twEventTable["kind"].Type != LuaValueType.Number)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}']['kind'] is not a TwitchEventKind. Check syntax. Aborting loading process ...");
                return null;
            }

            var kind = (TwitchTools.TwitchEventKind)twEventTable["kind"].Read<int>();
            if (twEventTable["action"].Type != LuaValueType.Function)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}']['action'] is not an action. Check syntax. Aborting loading process ...");
                return null;
            }

            var action = twEventTable["action"].Read<LuaFunction>();
            long? timeout = null;
            long cmdCost = 0;
            var perm = TwitchTools.PermissionLevel.Viewer;
            if (kind != TwitchTools.TwitchEventKind.TwitchReward)
            {
                if (twEventTable["timeout"].Type != LuaValueType.Number)
                {
                    Logger.Log(LogType.Warning, ServiceName,
                        $"In file {fileName} ['{currentKey}']['timeout'] is not a timeout. Will be used default value: {_stdTimeOut} ms");
                    timeout = _stdTimeOut;
                }
                else
                {
                    timeout = twEventTable["timeout"].Read<long>();
                }

                if (timeout < 0 && timeout != -1)
                {
                    Logger.Log(LogType.Warning, ServiceName,
                        $"In file {fileName} ['{currentKey}']['timeout'] is not a valid timeout. Will be used default value: {_stdTimeOut} ms");
                    timeout = _stdTimeOut;
                }

                if (twEventTable["perm"].Type != LuaValueType.Number)
                {
                    Logger.Log(LogType.Warning, ServiceName,
                        $"In file {fileName} ['{currentKey}']['perm'] is not a permission level. Will be used default value: Viewer");
                    perm = TwitchTools.PermissionLevel.Viewer;
                }
                else
                {
                    perm = (TwitchTools.PermissionLevel)twEventTable["perm"].Read<int>();
                }

                cmdCost = twEventTable["cmdCost"].Type != LuaValueType.Number
                    ? 0
                    : twEventTable["cmdCost"].Read<long>();
            }

            result.TryAdd((currentKey.ToString(), kind),
                new TwitchEvent(kind, action, currentKey.ToString(), perm, timeout, cmdCost));
            previosKey = kvp.Key;
        }

        return result;
    }

    public ConcurrentDictionary<string, TimerAction>? LoadTActions()
    {
        const string fileName = "TimerActions.lua";
        var fileResult = ParseLuaFile(fileName).GetAwaiter().GetResult();
        if (fileResult is null) return null;
        if (fileResult.HashMapCount == 0)
        {
            Logger.Log(LogType.Warning, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
            return [];
        }

        var actions = new ConcurrentDictionary<string, TimerAction>();
        var previosKey = LuaValue.Nil;

        while (fileResult.TryGetNext(previosKey, out var kvp))
        {
            var currentKey = kvp.Key;
            if (kvp.Value.Type != LuaValueType.Table)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}'] is not a TimerAction (expected table). Check syntax. Aborting loading process ...");
                return null;
            }

            var tActionTable = kvp.Value.Read<LuaTable>();

            var actionLua = tActionTable["action"];
            if (actionLua.Type != LuaValueType.Function)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}']['action'] is not an action (LuaFunction). Check syntax. Aborting loading process...");
                return null;
            }

            var action = actionLua.Read<LuaFunction>();

            var timeoutLua = tActionTable["timeout"];
            if (timeoutLua.Type != LuaValueType.Number)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}']['timeout'] is not a valid number. Check syntax. Aborting loading process...");
                return null;
            }

            var timeOut = timeoutLua.Read<long>();

            if (timeOut <= 0)
            {
                Logger.Log(LogType.Error, ServiceName,
                    $"In file {fileName} ['{currentKey}']['timeout'] ({timeOut}) is not in valid range. Allowed range (0, inf). Aborting loading process...");
                return null;
            }

            actions.TryAdd(currentKey.ToString(),
                new TimerAction { Function = action, Name = currentKey.ToString(), TimeOut = timeOut });
            previosKey = kvp.Key;
        }

        return actions;
    }

    // --- Static checker for configs ---

    public static bool CheckConfiguration()
    {
        return ConfigNames.All(config => File.Exists(Path.Combine(ConfigsPath, config)));
    }

    // --- Private block ---

    private async Task<LuaTable?> ParseLuaFile(string configName)
    {
        var fullPath = Path.Combine(ConfigsPath, configName);
        try
        {
            var result = await _lua.DoFileAsync(fullPath);
            if (result is not { Length: > 0 } || result[0].Type != LuaValueType.Table)
                throw new Exception($"Returned result form {configName} was not a valid table. Check syntax");
            return result[0].Read<LuaTable>();
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "During parsing lua file occured en error:", ex);
            return null;
        }
    }

    private static void GenerateTwitchEventsFile()
    {
        var filePath = Path.Combine(ConfigsPath, "TwitchEvents.lua");
        if (File.Exists(filePath)) return;
        Logger.Log(LogType.Info, ServiceName, "Generating twitch events file...");
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("local twitchevents = {}");
        sb.AppendLine("twitchevents['ping'] = {}");
        sb.AppendLine("twitchevents['ping']['kind'] = TwitchTools.TwitchEventKind('Command')");
        sb.AppendLine("twitchevents['ping']['action'] =");
        sb.AppendLine("    function(sender, args)");
        sb.AppendLine("        TwitchTools.SendMessage('@' .. sender .. ' -> pong')");
        sb.AppendLine("    end");
        sb.AppendLine("twitchevents['ping']['timeout'] = 1000 -- 1000 ms");
        sb.AppendLine("twitchevents['ping']['perm'] = TwitchTools.PermissionLevel('Viewer')");
        sb.AppendLine();
        sb.AppendLine("twitchevents['test'] = {}");
        sb.AppendLine("twitchevents['test']['kind'] = TwitchTools.TwitchEventKind('Reward')");
        sb.AppendLine("twitchevents['test']['action'] =");
        sb.AppendLine("    function(sender, args)");
        sb.AppendLine("        TwitchTools.SendMessage('@' .. sender .. ' -> test')");
        sb.AppendLine("    end");
        sb.AppendLine("return twitchevents");
        File.WriteAllText(filePath, sb.ToString());
    }

    private static void GenerateTimerActionsFile()
    {
        var filePath = Path.Combine(ConfigsPath, "TimerActions.lua");
        if (File.Exists(filePath)) return;
        Logger.Log(LogType.Info, ServiceName, "Generating timer actions file...");
        var sb = new StringBuilder();
        sb.AppendLine("local timeractions = {}");
        sb.AppendLine("");
        sb.AppendLine("--timeractions['test'] = {}");
        sb.AppendLine("--timeractions['test']['action'] =");
        sb.AppendLine("    --function()");
        sb.AppendLine("        --TwitchChat.SendMessage('Just a test -> test')");
        sb.AppendLine("    --end");
        sb.AppendLine("    --timeractions['test']['timeout'] = 10000 -- 10000 ms");
        sb.AppendLine("");
        sb.AppendLine("return timeractions");
        File.WriteAllText(filePath, sb.ToString());
    }

    private static void GenerateMainConfig()
    {
        var filePath = Path.Combine(ConfigsPath, "config.lua");
        if (File.Exists(filePath)) return;
        Logger.Log(LogType.Info, ServiceName, "Generating main config file...");
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
    private readonly LuaState _lua;
}