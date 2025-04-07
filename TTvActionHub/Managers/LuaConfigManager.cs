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

        public ConcurrentDictionary<string, TwitchCommand>? LoadCommands() 
        {
            var fileName = "commands.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable cmds) return null;
            if (cmds.Keys.Count == 0)
            {
                Logger.Warn($"Table from file {fileName} is empty. Ignoring...");
                return [];
            }

            var comannds = new ConcurrentDictionary<string, TwitchCommand>();

            foreach (var keyCommand in cmds.Keys)
            {
                if (cmds[keyCommand] is not LuaTable cmdTable)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyCommand}] is not a command. Check syntax. Aborting loading process...");
                    return null;
                }

                if (cmdTable["action"] is not LuaFunction action)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyCommand}]['action'] is not an action. Check syntax. Aborting loading process...");
                    return null;
                }

                if (cmdTable["timeout"] is not long timeOut)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} [{keyCommand}]['timeout'] is not valid timeout. Will be used default value: [{_stdTimeOut}] ms");
                    timeOut = _stdTimeOut;
                } 
                else if (timeOut < 0)
                {
                    if (timeOut != -1)
                    {
                        Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} [{keyCommand}]['timeout'] have value {timeOut}. It is not valid value. Will be used default value: [{_stdTimeOut}] ms");
                    }
                    timeOut = _stdTimeOut;
                }
                if (cmdTable["perm"] is not Users.USERLEVEL perm)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"In file {fileName} [{keyCommand}]['perm'] is not valid level of permissions. Will be used default value: [viewier]");
                    perm = Users.USERLEVEL.VIEWIER;
                }
                if (!comannds.TryAdd(keyCommand.ToString()!, new TwitchCommand() { Function = action, Perm = perm, TimeOut = timeOut }))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"For some reason [{keyCommand}] wasn't added to collection. Aborting loading process...");
                    return null;
                }
            }
            return comannds;
        }

        public ConcurrentDictionary<string, TwitchReward>? LoadRewards()
        {
            var fileName = "rewards.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable rwds) return null;
            if (rwds.Keys.Count == 0)
            {
                Logger.Warn($"Table from file {fileName} is empty. Ignoring...");
                return [];
            }

            var rewards = new ConcurrentDictionary<string, TwitchReward>();
            
            foreach ( var keyReward in rwds.Keys)
            {
                if (rwds[keyReward] is not LuaTable rewardTable)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyReward}] is not a reward. Check syntax. Aborting loading process...");
                    return null;
                }

                if (rewardTable["action"] is not LuaFunction action)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"In file {fileName} [{keyReward}]['action'] is not an action. Check syntax. Aborting loading process...");
                    return null;
                }

                if(!rewards.TryAdd(keyReward.ToString()!, new TwitchReward { Function = action }))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"For some reason [{keyReward}] wasn't added to collection. Aborting loading process...");
                    return null;
                }
            }
            return rewards;
        }

         public ConcurrentDictionary<string, TimerAction>? LoadTActions()
        {
            var fileName = "timeractions.lua";
            var fileResult = ParseLuaFile(fileName);
            if (fileResult is not LuaTable tactions) return null;
            if (tactions.Keys.Count == 0)
            {
                Logger.Warn($"Table from file {fileName} is empty. Ignoring...");
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
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").TwitchChat",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Storage",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Funcs",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Users"
            ];

        private static void GenerateCommandsFile()
        {
            var filePath = Path.Combine(ConfigsPath, "commands.lua");
            if (File.Exists(filePath)) return;
            StringBuilder builder = new();
            Bridges.ForEach(bridge => builder.AppendLine($"local {bridge.Split(").").Last()} = import {bridge}"));
            builder.AppendLine();
            builder.AppendLine("local commands = {}");
            builder.AppendLine();
            builder.AppendLine("commands['test'] = {}");
            builder.AppendLine("commands['test']['action'] = ");
            builder.AppendLine("\tfunction(sender, args)");
            builder.AppendLine("\t\tTwitchChat.SendMessage('@'..sender..' -> test')");
            builder.AppendLine("\tend");
            builder.AppendLine("commands['test']['timeout'] = 1000 -- 1000 ms");
            builder.AppendLine("commands['test']['perm'] = Users.USERLEVEL.VIEWIER");
            builder.AppendLine();
            builder.AppendLine("return commands");
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
            var filePath = Path.Combine(ConfigsPath, "rewards.lua");
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
            GenerateCommandsFile();
            GenerateRewardsFile();
            GenerateTimerActionsFile();
            GenerateMainConfig();
        }

        // --- Private fields ---
        private readonly Lua lua;
        private static readonly string _configPath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs")).FullName;
        private static readonly string[] _configNames = ["config.lua", "commands.lua", "rewards.lua", "timeractions.lua"];
    }
}
