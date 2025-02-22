using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLua;
using TwitchController.Items;
using TwitchController.Logs;
using TwitchController.Security;
using TwitchController.Twitch;

namespace TwitchController
{
    internal class Configuration
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public readonly (string Login, string ID, string Token) TwitchInfo;
        public readonly TwitchApi TwitchApi;


        public readonly long StandartCooldown;
        public readonly bool ShowLogs;

        public readonly Dictionary<string, Command> Commands;
        public readonly Dictionary<string, Reward> Rewards;

        public readonly string OpeningBracket;
        public readonly string ClosingBracket;

        public readonly string ConfigPath;

        public string FieldAdress(string field) => $"In [{ConfigPath}] field [{field}]";
        public string ParamAdress(string field, string param) => $"In [{ConfigPath}] parameter [{param} in field [{field}]";
        
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

            (string? Login, string? ID, string? Token) authInfo = (null, null, null);
            
            if (!isForceRelog)
            {
                authInfo = AuthorizationManager.LoadInfo(ClientSecret);
            }
            
            if (authInfo.Login == null || authInfo.ID == null || authInfo.Token == null)
            {
                authInfo = TwitchApi.GetAuthorizationInfo().Result;
            }
            
            if(authInfo.Login == null || authInfo.ID == null || authInfo.Token == null)
            {
                throw new Exception("Unable to get authorization info. Aborting");
            }

            TwitchInfo = new() { Login = authInfo.Login, ID = authInfo.ID, Token = authInfo.Token };

            AuthorizationManager.SaveInfo(ClientSecret, TwitchInfo.Login, TwitchInfo.ID, TwitchInfo.Token);
            
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

            LoadCommands(luaConfig["commands"] as LuaTable);
            LoadRewards(luaConfig["rewards"] as LuaTable);

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
                            
                Commands.Add(keyObj.ToString()!, new Command { Function = action, TimeOut = timer});
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
                    throw new Exception($"{ParamAdress("rewards", $"{keyObj}")} is not a command. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");
                
                Rewards.Add(keyObj.ToString()!, new Reward { Function = action });

                Logger.Info($"Loaded reward: {keyObj}");
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
@"local Keyboard = import('TwitchController', 'TwitchController.LuaTools.Hardware').Keyboard
local Mouse = import('TwitchController', 'TwitchController.LuaTools.Hardware').Mouse
local TwitchChat = import('TwitchController', 'TwitchController.LuaTools.Stuff').Chat
local Funcs = import('TwitchController', 'TwitchController.LuaTools.Stuff').Funcs

local res = {}
res[""force-relog""] = false -- may be changed to relogin with new account by force 
res[""timeout""] = 1000 -- may be changed
res[""logs""] = false -- may be changed

--res[""opening-bracket""] = '<' -- uncomment if you like !command <arg> more than !command (arg)
--res[""closing-bracket""] = '>' -- bracket may be any symbol, but to work they must be not identical

local commands = {}
local rewards = {}

res[""commands""] = commands
res[""rewards""] = rewards
return res"
                );
    }
}
