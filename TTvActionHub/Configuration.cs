using System.Text;
using NLua;
using System.Text;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Twitch;
using TTvActionHub.Authorization;
using TTvActionHub.LuaTools.Stuff;
using System.Collections.Concurrent;
using TwitchLib.Api.Core.Exceptions;

namespace TTvActionHub
{
    public class Configuration : IConfig
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public ConcurrentDictionary<string, Command> Commands { get => _commands; }
        public ConcurrentDictionary<string, TwitchReward> Rewards { get => _rewards; }
        public ConcurrentDictionary<string, TimerAction> TActions { get => _tActions; }

        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get => _ttvInfo; }
        public bool LogState { get => _logsState; }
        public (string obr, string cbr) Brackets { get => new(_obracket, _cbracket); }
        public TwitchApi TwitchApi { get => _twitchApi; }

        private (string Login, string ID, string Token, string RefreshToken) _ttvInfo;
        private readonly TwitchApi _twitchApi;

        private readonly long _stdCooldown;
        private readonly bool _logsState;
        private readonly bool _forceRelog;

        private ConcurrentDictionary<string, Command> _commands;
        private ConcurrentDictionary<string, TwitchReward> _rewards;
        private ConcurrentDictionary<string, TimerAction> _tActions;

        private readonly string _obracket;
        private readonly string _cbracket;

        private readonly string _configsPath;
        private readonly Lua? _lua;
        private static string FieldAdress(string confpath, string field) => $"In [{confpath}] field [{field}]";
        private static string ParamAdress(string confpath, string field, string param) => $"In [{confpath}] parameter [{param}] in field [{field}]";

        public Configuration(string configsPath)
        {

            _lua = new Lua();
            _lua.State.Encoding = Encoding.UTF8;
            _lua.LoadCLRPackage();
            _configsPath = configsPath;

            ReadConfigs(_configsPath, _lua, ["config", "commands", "rewards", "timeractions"], out var luaConfigsStates);

            _twitchApi = new TwitchApi(ClientId, ClientSecret, RedirectUrl);
            // --- Main field for config ---
            // --- They will not be readed again after reload ---
            var luaConfig = luaConfigsStates["config"];
            if (luaConfig["force-relog"] is not bool isForceRelog)
            {
                Logger.Warn($"In {FieldAdress("config", "force-relog")} is not presented. Will be used default value: [{false}].");
                _forceRelog = false;
            } 
            else _forceRelog = isForceRelog;

            if (luaConfig["opening-bracket"] is not string obracket || luaConfig["closing-bracket"] is not string cbracket)
            {
                Logger.Warn($"{FieldAdress("config", "opening-bracket")} or {FieldAdress("config", "closing-bracket")} is not presented. Ignoring...");
                obracket = String.Empty;
                cbracket = String.Empty;
            }

            if (luaConfig["logs"] is not bool logState)
            {
                Logger.Warn($"{FieldAdress("config", "logs")} is not presented. Will be used default value: [{true}].");
                logState = true;
            }
            
            if (luaConfig["timeout"] is not long timeOut)
            {
                timeOut = 30 * 1000; // 30 seconds in milliseconds
                Logger.Warn($"{FieldAdress("config", "timeout")} is not presented. Will be used default value: {timeOut}");
            }
            
            _obracket = obracket;
            _cbracket = cbracket;
            _logsState = logState;
            _stdCooldown = timeOut;

            // --- Reloadable fields ---

            _ttvInfo = AuthWithTwitch();
            _commands = LoadCommands(luaConfigsStates["commands"], "commands"); ;
            _rewards = LoadRewards(luaConfigsStates["rewards"], "rewards"); ;
            _tActions = LoadTActions(luaConfigsStates["timeractions"], "timeractions"); ;

            LogConfigStatus();
            
            Logger.Info($"Configuration loaded successfully");
        }

        public void ReloadConfig() 
        {
            ReadConfigs(_configsPath, _lua!, ["commands", "rewards", "timeractions"], out var luaConfigsStates);
            _ttvInfo = AuthWithTwitch();
            _commands = LoadCommands(luaConfigsStates["commands"], "commands"); ;
            _rewards = LoadRewards(luaConfigsStates["rewards"], "rewards"); ;
            _tActions = LoadTActions(luaConfigsStates["timeractions"], "timeractions"); ;

            LogConfigStatus();
        }

        private (string Login, string ID, string Token, string RefreshToken) GetAuthInfoFromAPI()
        {
            (string Login, string ID, string Token, string RefreshToken) authInfo = new();
            var authTask = _twitchApi.GetAuthorizationInfo();
            authTask.Wait();
            if (authTask.IsCompleted)
            {
                var (Token, RefreshToken) = authTask.Result;
                if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(RefreshToken))
                    throw new BadRequestException("Unable to get Authorization information");
                authInfo.Token = Token;
                authInfo.RefreshToken = RefreshToken;
            }
            var channelInfoTask = _twitchApi.GetChannelInfoAsync(authInfo.Token!);
            channelInfoTask.Wait();
            if (channelInfoTask.IsCompleted)
            {
                var (Login, ID) = channelInfoTask.Result;
                if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(ID))
                    throw new BadRequestException("Unable to get channel information");
                authInfo.Login = Login;
                authInfo.ID = ID;
            }

            return authInfo;
        }

        private ConcurrentDictionary<string, Command> LoadCommands(LuaTable? cmds, string path)
        {
            var commands = new ConcurrentDictionary<string, Command>();
            if (cmds is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return commands;
            }

            foreach (var keyObj in cmds.Keys)
            {

                if (cmds[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress(path, "commands", keyObj.ToString()!)} is not a command. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "action")} is not a action. Check syntax.");

                if (table["timeout"] is not long timer)
                {
                    Logger.Warn($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not presented. Will be used default value: {_stdCooldown} ms");
                    timer = _stdCooldown;
                }
                else if (timer < 0)
                {
                    Logger.Warn($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not a valid timeout value. Will be used default value: {_stdCooldown} ms");
                    timer = _stdCooldown;
                }
                if (table["perm"] is not Users.USERLEVEL perm)
                {
                    Logger.Warn($"{ParamAdress(path, keyObj.ToString()!, "perm")} is not presented. Will be used default value: VIEWIER");
                    perm = Users.USERLEVEL.VIEWIER;
                }
                            
                commands.TryAdd(keyObj.ToString()!, new Command { Function = action, Perm = perm, TimeOut = timer });
                Logger.Info($"Loaded comand: {keyObj}");
            }

            return commands;
        }

        private ConcurrentDictionary<string, TwitchReward> LoadRewards(LuaTable? rwrds, string path)
        {
            var rewards = new ConcurrentDictionary<string, TwitchReward>();
            if (rwrds is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return rewards;
            }
            
            foreach (var keyObj in rwrds.Keys)
            {
                if (rwrds[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress(path, "rewards", keyObj.ToString()!)} is not a reward. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "action")} is not a action. Check syntax.");
                
                rewards.TryAdd(keyObj.ToString()!, new TwitchReward { Function = action });

                Logger.Info($"Loaded reward: {keyObj}");
            }
            return rewards;
        }

        private ConcurrentDictionary<string, TimerAction> LoadTActions(LuaTable? events, string path)
        {
            var actions = new ConcurrentDictionary<string, TimerAction>();
            if (events is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return actions;
            }

            foreach (var keyObj in events.Keys)
            {
                if (events[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress(path, "tactions", keyObj.ToString()!)} is not a timer action. Check syntax.");
                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "action")} is not a action. Check syntax.");
                if (table["timeout"] is not long timeout)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not a integer. Check syntax.");
                else if (timeout <= 0)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not valid time. Allowed values (>= 1).");

                actions.TryAdd(keyObj.ToString()!, new TimerAction() { Action = action, Name = keyObj.ToString()!, TimeOut = timeout });

                Logger.Info($"Loaded TimerAction: {keyObj}");
            }
            return actions;
        }

        private void LogConfigStatus()
        {
            Logger.Info($"Login: {_ttvInfo.Login}");
            Logger.Info($"ID: {_ttvInfo.ID}");
            Logger.Info($"Token: found");
            Logger.Info($"Standart cooldown: {_stdCooldown}");
            Logger.Info($"Services logs state: {_logsState}");
            Logger.Info($"Loaded commands: [{string.Join(',', _commands.Keys)}]");
            Logger.Info($"Loaded rewards: [{string.Join(',', _rewards.Keys)}]");
            if (!string.IsNullOrEmpty(_obracket) && !string.IsNullOrEmpty(_cbracket))
                Logger.Info($"Brackets: {_obracket} and {_cbracket}");

        }

        private void ReadConfigs(string configsPath, Lua lua, IEnumerable<string> confNames, out Dictionary<string, LuaTable> outTable)
        {
            outTable = [];
            foreach (var confName in confNames)
            {
                var confPath = Path.Combine(configsPath, $"{confName}.lua");
                var confState = lua.DoFile(confPath);
                if (confState[0] is not LuaTable table)
                {
                    throw new Exception($"Returned result form {confPath} was not a valid table. Checl syntax");
                }
                outTable.Add(confName, table);
            }
        }

        private (string Login, string ID, string Token, string RefreshToken) AuthWithTwitch()
        {
            AuthManager manager = new(_twitchApi, ClientSecret);
            if (_forceRelog || !manager.LoadTwitchInfo())
            {
                var result = GetAuthInfoFromAPI();
                manager.TwitchInfo = result;
                manager.SaveTwitchInfo();
                return result;
            }
            var validationTask = manager.IsValidTokensAsync();
            validationTask.Wait();
            if (!validationTask.Result)
            {
                try
                {
                    var refreshTask = manager.UpdateAuthInfoAsync();
                    refreshTask.Wait();
                    if (!refreshTask.Result)
                    {
                        manager.TwitchInfo = GetAuthInfoFromAPI();
                    }
                } catch (Exception ex)
                {
                    Logger.Error($"Unable to update Twitch Info with manager due to error", ex);
                    Logger.Info($"Getting new Twitch Info from browser");
                    manager.TwitchInfo = GetAuthInfoFromAPI();
                }
                finally
                {
                    manager.SaveTwitchInfo();
                }
            }
            return manager.TwitchInfo;
        }
    
    }
}
