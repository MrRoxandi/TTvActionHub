using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLua;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Security;
using TTvActionHub.Twitch;

namespace TTvActionHub
{
    public class Configuration
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public readonly (string Login, string ID, string Token, string RefreshToken) TwitchInfo;
        public readonly TwitchApi TwitchApi;


        public readonly long StandartCooldown;
        public readonly bool ShowLogs;

        public readonly Dictionary<string, Command> Commands;
        public readonly Dictionary<string, Reward> Rewards;
        public readonly List<TEvent> TEvents;

        public readonly string OpeningBracket;
        public readonly string ClosingBracket;

        public readonly string ConfigPath;

        public string FieldAdress(string field) => $"In [{ConfigPath}] field [{field}]";
        public string ParamAdress(string field, string param) => $"In [{ConfigPath}] parameter [{param}] in field [{field}]";
        
        public Configuration(string path)
        {

            var lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();

            var state = lua.DoFile(path);
            if (state[0] is not LuaTable luaConfig)
            {
                throw new Exception($"Failed to load controller configuration. Check the [{path}] config");
            }
            ConfigPath = path;

            TwitchApi = new TwitchApi(ClientId, ClientSecret, RedirectUrl);

            if (luaConfig["force-relog"] is not bool isForceRelog)
            {
                Logger.Warn($"{FieldAdress("force-relog")} is not presented. Will be used default value: [{false}].");
                isForceRelog = false;
            }

            (string? Login, string? ID, string? Token, string? RefreshToken)? authInfo = null;

            if (!isForceRelog)
            {
                authInfo = AuthorizationManager.LoadInfo(ClientSecret);
                if (authInfo is (string, string, string, string) info)
                {
                    if (!TwitchApi.ValidateTokenAsync(info.Token).Result)
                    {
                        Logger.Info("Trying to update token");
                        var (AccessToken, RefreshToken) = TwitchApi.RefreshAccessTokenAsync(info.RefreshToken).Result;
                        info.Token = AccessToken;
                        info.RefreshToken = RefreshToken ?? authInfo?.RefreshToken;
                    }

                    authInfo = info;
                }
            }

            if(isForceRelog || authInfo == null)
            {
                var auth = TwitchApi.GetAuthorizationInfo().Result;
                if (auth.Login == null || auth.ID == null || auth.Token == null)
                    throw new Exception("Unable to get Authorization information");
                authInfo = auth;
            }

            TwitchInfo = new() { 
                Login = authInfo?.Login ?? "", 
                ID = authInfo?.ID ?? "", 
                Token = authInfo?.Token ?? "", 
                RefreshToken = authInfo?.RefreshToken ?? "" 
            };

            

            AuthorizationManager.SaveInfo(ClientSecret, TwitchInfo.Login, TwitchInfo.ID, TwitchInfo.Token, TwitchInfo.RefreshToken);
            
            if (luaConfig["opening-bracket"] is not string obracket || luaConfig["closing-bracket"] is not string cbracket)
            {
                //TODO: FIX
                //ConsoleLogger.Warn($"{FieldAdress("[opening-bracket] or [closing-bracket]")} is not presented. Ignoring...");
                obracket = String.Empty;
                cbracket = String.Empty;
            }

            OpeningBracket = obracket;
            ClosingBracket = cbracket;

            if (luaConfig["logs"] is not bool logState)
            {
                Logger.Warn($"{FieldAdress("logs")} is not presented. Will be used default value: [{true}].");
                logState = true;
            }
            
            ShowLogs = logState;
            
            if (luaConfig["timeout"] is not long timeOut)
            {
                timeOut = 30 * 1000; // 30 seconds in milliseconds
                Logger.Warn($"{FieldAdress("timeout")} is not presented. Will be used default value: {timeOut}");
            }
            
            StandartCooldown = timeOut;

            Commands = [];
            Rewards = [];
            TEvents = [];

            LoadCommands(luaConfig["commands"] as LuaTable);
            LoadRewards(luaConfig["rewards"] as LuaTable);
            LoadTEvents(luaConfig["tevents"] as LuaTable);

            ShowConfigInfo();

            Logger.Info($"Configuration loaded successfully");
        }

        private void LoadCommands(LuaTable? cmds) {
            
            if (cmds is null) {
                Logger.Warn($"{FieldAdress("commands")} is not presented. Ignoring...");
                return;
            }

            foreach (var keyObj in cmds.Keys)
            {

                if (cmds[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("commands", $"{keyObj}")} is not a command. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");

                if (table["timeout"] is not long timer)
                {
                    Logger.Warn($"{ParamAdress($"{keyObj}", "timeout")} is not presented. Will be used default value: {StandartCooldown} ms");
                    timer = StandartCooldown;
                }
                if (table["perm"] is not USERLEVEL perm)
                {
                    Logger.Warn($"{ParamAdress($"{keyObj}", "perm")} is not presented. Will be used default value: VIEWIER");
                    perm = USERLEVEL.VIEWIER;
                }
                            
                Commands.Add(keyObj.ToString()!, new Command { Function = action, Perm = perm, TimeOut = timer});
                Logger.Info($"Loaded comand: {keyObj}");
            }
        }

        private void LoadRewards(LuaTable? rewards)
        {
            if (rewards is null)
            {
                Logger.Warn($"{FieldAdress("rewards")} is not presented. Ignoring...");
                return;
            }
            
            foreach (var keyObj in rewards.Keys)
            {
                if (rewards[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("rewards", $"{keyObj}")} is not a reward. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");
                
                Rewards.Add(keyObj.ToString()!, new Reward { Function = action });

                Logger.Info($"Loaded reward: {keyObj}");
            }
        }

        private void LoadTEvents(LuaTable? events)
        {
            if (events is null)
            {
                Logger.Warn($"{FieldAdress("tevents")} is not presented. Ignoring...");
                return;
            }

            foreach (var keyObj in events.Keys)
            {
                if(events[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("tevents", $"{keyObj}")} is not a event. Check syntax.");
                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");
                if (table["timeout"] is not long timeout)
                    throw new Exception($"{ParamAdress($"{keyObj}", "timeout")} is not a integer. Check syntax.");
                else if(timeout <= 0)
                    throw new Exception($"{ParamAdress($"{keyObj}", "timeout")} is not valid time. Allowed values (>= 1).");

                TEvents.Add(new TEvent() { Action = action, Name = keyObj.ToString()!, TimeOut = timeout });

                Logger.Info($"Loaded TEvent: {keyObj}");
            }
        }

        private void ShowConfigInfo()
        {
            Logger.Info($"Config file stored at: {ConfigPath}");
            Logger.Info($"Login: {TwitchInfo.Login}");
            Logger.Info($"ID: {TwitchInfo.ID}");
            Logger.Info($"Token: found");
            Logger.Info($"Standart cooldown: {StandartCooldown}");
            Logger.Info($"Services logs state: {ShowLogs}");
            if (!string.IsNullOrEmpty(OpeningBracket) && !string.IsNullOrEmpty(ClosingBracket))
                Logger.Info($"Brackets: {OpeningBracket} and {ClosingBracket}");

        }

        public static void GenerateConfig(string path) => File.WriteAllText(path,
@"
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
local Chat = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Chat
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs

local res = {}
res[""force-relog""] = false -- may be changed to relogin with new account by force 
res[""timeout""] = 1000 -- may be changed
res[""logs""] = false -- may be changed

--res[""opening-bracket""] = '<' -- uncomment if you like !command <arg> more than !command (arg)
--res[""closing-bracket""] = '>' -- bracket may be any symbol, but to work they must be not identical

local commands = {}
local rewards = {}
loacl tevents = {}

res[""commands""] = commands
res[""rewards""] = rewards
res[""tevents""] = tevents
return res"
                );
    }
}
