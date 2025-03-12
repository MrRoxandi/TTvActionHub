using System.Text;
using NLua;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Security;
using TTvActionHub.Twitch;
using TTvActionHub.LuaTools.Stuff;
using System.Collections.Concurrent;

namespace TTvActionHub
{
    public class Configuration : IConfig
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public ConcurrentDictionary<string, Command> Commands { get => _commands; }
        public ConcurrentDictionary<string, Reward> Rewards { get => _rewards; }
        public List<TimerAction> TActions { get => _tActions; }

        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get => _ttvInfo; }
        public bool LogState { get => _logsState; }
        public (string obr, string cbr) Brackets { get => new(_obracket, _cbracket); }
        public TwitchApi TwitchApi { get => _twitchApi; }

        private readonly (string Login, string ID, string Token, string RefreshToken) _ttvInfo;
        private readonly TwitchApi _twitchApi;

        private readonly long _stdCooldown;
        private readonly bool _logsState;

        private readonly ConcurrentDictionary<string, Command> _commands;
        private readonly ConcurrentDictionary<string, Reward> _rewards;
        private readonly List<TimerAction> _tActions;

        private readonly string _obracket;
        private readonly string _cbracket;

        private static string FieldAdress(string confpath, string field) => $"In [{confpath}] field [{field}]";
        private static string ParamAdress(string confpath, string field, string param) => $"In [{confpath}] parameter [{param}] in field [{field}]";

        public Configuration(string configs_path)
        {

            var lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();

            var configpath = Path.Combine(configs_path, "config.lua");
            var commandspath = Path.Combine(configs_path, "commands.lua");
            var rewardspath = Path.Combine(configs_path, "rewards.lua");
            var timeractionspath = Path.Combine(configs_path, "timeractions.lua");

            var config_state = lua.DoFile(configpath);
            var commands_state = lua.DoFile(commandspath);
            var rewards_state = lua.DoFile(rewardspath);
            var timeraction_state = lua.DoFile(timeractionspath);
            
            if (config_state[0] is not LuaTable luaConfig)
            {
                throw new Exception($"Returned result from {configpath} was not a valid table. Check syntax.");
            }

            if (commands_state[0] is not LuaTable luaCommands)
            {
                throw new Exception($"Returned result from {commandspath} was not a valid table. Check syntax.");
            }

            if (rewards_state[0] is not LuaTable luaRewards)
            {
                throw new Exception($"Returned result from {rewardspath} was not a valid table. Check syntax.");
            }

            if (timeraction_state[0] is not LuaTable luaTimerActions)
            {
                throw new Exception($"Returned result from {timeractionspath} was not a valid table. Check syntax.");
            }

            _twitchApi = new TwitchApi(ClientId, ClientSecret, RedirectUrl);

            if (luaConfig["force-relog"] is not bool isForceRelog)
            {
                Logger.Warn($"{FieldAdress(configpath, "force-relog")} is not presented. Will be used default value: [{false}].");
                isForceRelog = false;
            } 

            (string? Login, string? ID, string? Token, string? RefreshToken) authInfo = new();

            if (isForceRelog)
            {
                authInfo = GetAuthInfoFromAPI();
            }
            else
                {
                var authManagerResult = AuthorizationManager.LoadInfo(ClientSecret);
                if (authManagerResult is null)
                    authInfo = GetAuthInfoFromAPI();
                else authInfo = authManagerResult.Value;
                var valid_task = _twitchApi.ValidateTokenAsync(authInfo.Token!);
                valid_task.Wait();
                if (!valid_task.Result)
                    {
                    var refresh_task = _twitchApi.RefreshAccessTokenAsync(authInfo.RefreshToken!);
                    refresh_task.Wait();
                    authInfo.Token = refresh_task.Result.AccessToken;
                    authInfo.RefreshToken = refresh_task.Result.RefreshToken;
                    }
                }

            if (authInfo.Token is not string token ||
                authInfo.RefreshToken is not string rtoken ||
                authInfo.Login is not string login ||
                authInfo.ID is not string id)
            {
                throw new Exception("For some reasong program failed to get auth info. If u see this error report it");
            }
            else
            {
                _ttvInfo = new() { ID = id, Login = login, RefreshToken = rtoken, Token = token };
            }

            
            AuthorizationManager.SaveInfo(ClientSecret, _ttvInfo.Login, _ttvInfo.ID, _ttvInfo.Token, _ttvInfo.RefreshToken);
            
            if (luaConfig["opening-bracket"] is not string obracket || luaConfig["closing-bracket"] is not string cbracket)
            {
                Logger.Warn($"{FieldAdress(configpath, "opening-bracket")} or {FieldAdress(configpath, "closing-bracket")} is not presented. Ignoring...");
                obracket = String.Empty;
                cbracket = String.Empty;
            }

            _obracket = obracket;
            _cbracket = cbracket;

            if (luaConfig["logs"] is not bool logState)
            {
                Logger.Warn($"{FieldAdress(configpath, "logs")} is not presented. Will be used default value: [{true}].");
                logState = true;
            }
            
            _logsState = logState;
            
            if (luaConfig["timeout"] is not long timeOut)
            {
                timeOut = 30 * 1000; // 30 seconds in milliseconds
                Logger.Warn($"{FieldAdress(configpath, "timeout")} is not presented. Will be used default value: {timeOut}");
            }
            
            _stdCooldown = timeOut;

            _commands = [];
            _rewards = [];
            _tActions = [];

            LoadCommands(luaCommands, commandspath);
            LoadRewards(luaRewards, rewardspath);
            LoadTActions(luaTimerActions, timeractionspath);

            ShowConfigInfo();

            Logger.Info($"Configuration loaded successfully");
        }

        private (string? Login, string? ID, string? Token, string? RefreshToken) GetAuthInfoFromAPI()
        {
            (string? Login, string? ID, string? Token, string? RefreshToken) authInfo = new();
            var authTask = _twitchApi.GetAuthorizationInfo();
            authTask.Wait();
            if (authTask.IsCompleted)
            {
                var (Token, RefreshToken) = authTask.Result;
                if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(RefreshToken))
                    throw new Exception("Unable to get Authorization information");
                authInfo.Token = Token;
                authInfo.RefreshToken = RefreshToken;
            }
            var channelInfoTask = _twitchApi.GetChannelInfoAsync(authInfo.Token!);
            channelInfoTask.Wait();
            if (channelInfoTask.IsCompleted)
            {
                var (Login, ID) = channelInfoTask.Result;
                if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(ID))
                    throw new Exception("Unable to get channel information");
                authInfo.Login = Login;
                authInfo.ID = ID;
            }

            return authInfo;
        }

        private void LoadCommands(LuaTable? cmds, string path)
        {
            
            if (cmds is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return;
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
                            
                _commands.TryAdd(keyObj.ToString()!, new Command { Function = action, Perm = perm, TimeOut = timer });
                Logger.Info($"Loaded comand: {keyObj}");
            }
        }

        private void LoadRewards(LuaTable? rewards, string path)
        {
            if (rewards is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return;
            }
            
            foreach (var keyObj in rewards.Keys)
            {
                if (rewards[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress(path, "rewards", keyObj.ToString()!)} is not a reward. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "action")} is not a action. Check syntax.");
                
                _rewards.TryAdd(keyObj.ToString()!, new Reward { Function = action });

                Logger.Info($"Loaded reward: {keyObj}");
            }
        }

        private void LoadTActions(LuaTable? events, string path)
        {
            if (events is null)
            {
                Logger.Warn($"Table from file {path} is empty. Ignoring...");
                return;
            }

            foreach (var keyObj in events.Keys)
            {
                if (events[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress(path, "tactions", keyObj.ToString()!)} is not a event. Check syntax.");
                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "action")} is not a action. Check syntax.");
                if (table["timeout"] is not long timeout)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not a integer. Check syntax.");
                else if (timeout <= 0)
                    throw new Exception($"{ParamAdress(path, keyObj.ToString()!, "timeout")} is not valid time. Allowed values (>= 1).");

                _tActions.Add(new TimerAction() { Action = action, Name = keyObj.ToString()!, TimeOut = timeout });

                Logger.Info($"Loaded TEvent: {keyObj}");
            }
        }

        private void ShowConfigInfo()
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

    }
}
