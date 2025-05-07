using NLua;
using System.Collections.Concurrent;
using System.Text;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Stuff;

namespace TTvActionHub.Managers
{
    public class LuaConfigManager
    {
        // --- Public block ---

        public static string ConfigsPath { get => _configPath; }
        public static string ServiceName { get => "LuaConfigManager"; }
        public static IEnumerable<string> ConfigNames { get => _configNames; }
        
        public bool ForceRelog { get => _forceRelog; }
        public bool MoreLogs { get => _moreLogs; }

        private readonly bool _forceRelog;
        private readonly long _stdTimeOut;
        private readonly bool _moreLogs;

        public LuaConfigManager() 
        { 
            lua = new Lua();
            lua.LoadCLRPackage();
            lua.State.Encoding = Encoding.UTF8;
            var fileName = "config.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable configParams)
            {
                throw new Exception($"File {fileName} is not a proper config. Check syntax.");
            }
            if (configParams["force-reload"] is not bool forceRelog)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['force-relog'] is not presented. Will be used default value: [{false}]");
                _forceRelog = false;
            }
            else _forceRelog = forceRelog;

            if (configParams["logs"] is not bool moreLogs)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['logs'] is not presented. Will be used default value: [{false}]");
                _moreLogs = false;
            }
            else _moreLogs = moreLogs;

            if (configParams["timeout"] is not long stdTimeOut)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['timeout'] is not presented. Will be used default value: [{30000}] ms");
                _stdTimeOut = 30000;
            }
            else if (stdTimeOut < 0)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['timeout'] have [{stdTimeOut}] value. But valid value must be > 0. Will be used default value: [{30000}] ms");
                _stdTimeOut = 30000;
            }
            else _stdTimeOut = stdTimeOut;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Configuration loaded successfully");
        }

        public ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>? LoadTwitchEvents()
        {
            var fileName = "twitchevents.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable events) return null;

            if (events.Keys.Count == 0)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
                return [];
            }

            var result = new ConcurrentDictionary<(string, TwitchTools.TwitchEventKind), TwitchEvent>();
            foreach (var key in events.Keys)
            {
                if (events[key] is not LuaTable TwEventTable)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} ['{key}'] is not a TwitchEvent. Check syntax. Aborting loading process ...");
                    return null;
                }

                if (TwEventTable["kind"] is not TwitchTools.TwitchEventKind kind)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} ['{key}']['kind'] is not a TwitchEventKind. Check syntax. Aborting loading process ...");
                    return null;
                }

                if (TwEventTable["action"] is not LuaFunction action)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} ['{key}']['action'] is not an action. Check syntax. Aborting loading process ...");
                    return null;
                }

                long? timeOut = null;
                TwitchTools.PermissionLevel perm = TwitchTools.PermissionLevel.VIEWIER;
                if (kind != TwitchTools.TwitchEventKind.TwitchReward)
                {
                    if (TwEventTable["timeout"] is not long time)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['{key}']['timeout'] is not a timeout. Will be used defult value: {_stdTimeOut} ms");
                        time = _stdTimeOut;
                    }
                    timeOut = time;

                    if (TwEventTable["perm"] is not TwitchTools.PermissionLevel userLevel)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} ['{key}']['perm'] is not a permission level. Will be used defult value: VIEWIER");
                        userLevel = TwitchTools.PermissionLevel.VIEWIER;
                    }

                    perm = userLevel;
                }

                result.TryAdd((key.ToString()!, kind), new(kind, action, key.ToString()!, perm, timeOut));
            }

            return result;
        }

        public ConcurrentDictionary<string, TimerAction>? LoadTActions()
        {
            var fileName = "timeractions.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable tactions) return null;
            if (tactions.Keys.Count == 0)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, $"Table from file {fileName} is empty. Ignoring...");
                return [];
            }
            var actions = new ConcurrentDictionary<string, TimerAction>();
            foreach (var keyTAction in tactions.Keys)
            {
                if (tactions[keyTAction] is not LuaTable tActionTable)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyTAction}] is not a TimerAction. Check syntax. Aborting loading process ...");
                    return null;
                }

                if (tActionTable["action"] is not LuaFunction action)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyTAction}]['action'] is not an action. Check syntax. Aborting loading process...");
                    return null;
                }

                if (tActionTable["timeout"] is not long timeOut)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyTAction}]['timeout'] is not valid value. Check syntax. Aborting loading process...");
                    return null;
                } 
                else if (timeOut <= 0)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyTAction}]['timeout'] is not in valid range. Allowed range [1, inf). Aborting loading process...");
                    return null;
                }

                if (!actions.TryAdd(keyTAction.ToString()!, new TimerAction() { Function = action, Name = keyTAction.ToString()!, TimeOut = timeOut }))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"For some reason [{keyTAction}] wasn't added to collection. Aborting loading process...");
                    return null;
                }
            }
            return actions;
        }

        // --- Static checker for configs ---

        public static bool CheckConfiguration() => ConfigNames.All(config => File.Exists(Path.Combine(ConfigsPath, config)));

        // --- Private block ---

        private LuaTable? ParseLuaFile(string configName)
        {
            var fullPath = Path.Combine(ConfigsPath, configName);
            try
            {
                var result = lua.DoFile(fullPath);
                var state = result[0];
                if (state is not LuaTable table)
                {
                    throw new Exception($"Returned result form {configName} was not a valid table. Check syntax");
                }
                return table;
            }
            catch (Exception ex) 
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "During parsing lua file occured en erorr:", ex);
                return null;
            }
        }

        // All avaliable static bridges for lua
        private static List<string> Bridges => [
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Audio\").Sounds",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Keyboard",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Mouse",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").TwitchTools",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Storage",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Funcs",
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
            builder.AppendLine("twitchevents['ping']['perm'] = TwitchTools.PermissionLevel.VIEWIER");
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

        private static void GenerateRewardsFile()
        {
            var filePath = Path.Combine(ConfigsPath, "rewards.lua");
            if (File.Exists(filePath)) return;
            StringBuilder builder = new();
            Bridges.ForEach(bridge => builder.AppendLine($"local {bridge.Split(").").Last()} = import {bridge}"));
            builder.AppendLine();
            builder.AppendLine("local rewards = {}");
            builder.AppendLine();
            builder.AppendLine("rewards['test'] = {}");
            builder.AppendLine("rewards['test']['action'] =");
            builder.AppendLine("\tfunction(sender, args)");
            builder.AppendLine("\t\tTwitchChat.SendMessage('@'..sender..' -> test')");
            builder.AppendLine("\tend");
            builder.AppendLine();
            builder.AppendLine("return rewards");
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
            builder.AppendLine("--configuration['opening-bracket'] = '<'");
            builder.AppendLine("--configuration['closing-bracket'] = '<'");
            builder.AppendLine();
            builder.AppendLine("return configuration");
            File.WriteAllText(filePath, builder.ToString());
        }

        public static void GenerateAllConfigs()
        {
            GenerateTwitchEventsFile();
            //GenerateRewardsFile();
            GenerateTimerActionsFile();
            GenerateMainConfig();
        }

        // --- Private fields ---
        private readonly Lua lua;
        private static readonly string _configPath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs")).FullName;
        private static readonly string[] _configNames = ["config.lua", "twitchevents.lua", "timeractions.lua"];
    }
}
