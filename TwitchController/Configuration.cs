using System;
using System.Collections.Generic;
using System.Text;
using NLua;
using TwitchController.Items;
using TwitchController.Security;
using TwitchController.Twitch;

namespace TwitchController
{
    public class Configuration
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public readonly (string Login, string ID, string Token) TwitchInfo;
        public readonly TwitchApiService TwitchApi;


        public readonly long GlobalTimeOut;
        public readonly bool ShowLogs;

        public readonly Dictionary<string, Command> Commands;
        public readonly Dictionary<string, Reward> Rewards;

        public readonly string? OpeningBracket;
        public readonly string? ClosingBracket;

        public Configuration(string path)
        {

            var lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();

            var state = lua.DoFile(path);
            if (state[0] is not LuaTable luaConfig)
            {
                throw new Exception("Failed to load controller configuration");
            }

            TwitchApi = new TwitchApiService(ClientId, ClientSecret, RedirectUrl);
            if (luaConfig["force-relog"] is not bool isForceRelog) isForceRelog = false;
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
                throw new Exception("Unable to get authorizationinfo. Aborting");
            }

            TwitchInfo = new() { Login = authInfo.Login, ID = authInfo.ID, Token = authInfo.Token };

            AuthorizationManager.SaveInfo(ClientSecret, TwitchInfo.Login, TwitchInfo.ID, TwitchInfo.Token);
            
            Console.WriteLine($"[INFO]\n-- TwitchChannel: {TwitchInfo.Login}\n-- Token: Found");
           
            OpeningBracket = luaConfig["opening-bracket"] as string;
            ClosingBracket = luaConfig["closing-bracket"] as string;

            if (luaConfig["logs"] is not bool)
            {
                Console.WriteLine($"[WARN] Parameter {{logs}} in {path} not specified. Using default state: {true}");
                ShowLogs = true;
            }
            else ShowLogs = false;

            Console.WriteLine($"[INFO] Logs state: {ShowLogs}");
            if (luaConfig["timeout"] is not long timeOut)
            {
                GlobalTimeOut = 30 * 1000; // 30 seconds in milliseconds
                Console.WriteLine($"[WARN] Parameter {{timeout}} in {path} not specified. Using default state: {GlobalTimeOut}ms");
            }
            else GlobalTimeOut = timeOut;

            Console.WriteLine($"[INFO] Timeout state: {GlobalTimeOut} ms");

            Commands = [];
            Rewards = [];

            LoadCommands(luaConfig["commands"] as LuaTable);
            LoadRewards(luaConfig["rewards"] as LuaTable);

            Console.WriteLine($"[INFO] Configuration loaded successfully!");
        }

        private void LoadCommands(LuaTable? cmds) {
            
            if (cmds is null) {
                Console.WriteLine($"[WARN] Parameter {{commands}} in not specified. Skipping...");
                return;
            }

            Console.WriteLine($"[INFO] Started loading commands: ");
            
            foreach (var keyObj in cmds.Keys)
            {

                if (cmds[keyObj] is not LuaTable table)
                    throw new Exception($"Failed to load command: {keyObj}");

                var timeout = GlobalTimeOut;

                //Setting timeout for command
                if (table["timeout"] is long timer)
                    timeout = timer;

                //Setting description for commands
                var desc = table["description"] as string;

                //Setting action for command. Must be presented
                if (table["action"] is not LuaFunction action)
                    throw new Exception($"Unable to find field {{action}} for command {keyObj}");

                Commands.Add(keyObj.ToString()!, new Command { Function = action, TimeOut = timeout, Description = desc });
                Console.WriteLine($"  -- Loaded command: {keyObj}");
            }
        }

        private void LoadRewards(LuaTable? rewards)
        {
            if (rewards is null)
            {
                Console.WriteLine($"[WARN] Parameter {{rewards}} in not specified. Skipping...");
                return;
            }
            Console.WriteLine($"[INFO] Started loading rewards: ");

            foreach (var keyObj in rewards.Keys)
            {
                if (rewards[keyObj] is not LuaTable table)
                    throw new Exception($"Failed to load reward: {keyObj}");

               //Setting description for reward
                var desc = table["description"] as string;

                //Setting action for rewards. Must be presented
                if(table["action"] is not LuaFunction action)
                    throw new Exception($"Failed to load action for reward: {keyObj}");

                Rewards.Add(keyObj.ToString()!, new Reward { Function = action, Description = desc });
                Console.WriteLine($"  -- Loaded reward: {keyObj}");
            }
        }

        public static void GenerateConfig(string path) => File.WriteAllText(path,
@"local Keyboard = import('TwitchController', 'TwitchController.Hardware').Keyboard
local Mouse = import('TwitchController', 'TwitchController.Hardware').Mouse
local TwitchChat = import('TwitchController', 'TwitchController.Stuff').Chat

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
