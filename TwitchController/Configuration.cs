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
        private readonly string _clientId = "hd9kavndkos83ujswrqhuffa90kcb6";
        private readonly string _clientSecret = "nv5bgx5a4321lopr6knf0acek8b2e1";
        private readonly string _redirectUri = @"http://localhost:3000/";

        public readonly string TwitchChannel;
        public readonly string TwitchBotName;
        public readonly string Token;

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

            if (luaConfig["botname"] is not string botName)
            {
                throw new Exception($"Unable to get botname from file: {path}");
            }
            TwitchBotName = botName;

            if (luaConfig["channel"] is not string channel)
            {
                throw new Exception($"Unable to get channel from file: {path}");
            }
            TwitchChannel = channel;
            
            Token = TokenManager.LoadToken(botName) ?? GetNewTokenAsync().Result;
            TokenManager.SaveToken(botName, Token);
            
            Console.WriteLine($"[INFO]\n-- TwitchChannel: {TwitchChannel}\n-- Token: Found");
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

        private async Task<string> GetNewTokenAsync()
        {
            var twitchService = new TwitchApiService(_clientId, _clientSecret, _redirectUri);
            string? token = await twitchService.StartAuthFlowAsync();

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Failed to authenticate with Twitch API");
            }
            return token;
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

                var timeout = GlobalTimeOut;

                //Setting timeout for reward
                if (table["timeout"] is long timer)
                    timeout = timer;

                //Setting description for reward
                var desc = table["description"] as string;

                //Setting action for rewards. Must be presented
                var action = table["action"] as LuaFunction;

                Rewards.Add(keyObj.ToString()!, new Reward { Function = action, TimeOut = timeout, Description = desc });
                Console.WriteLine($"  -- Loaded reward: {keyObj}");
            }
        }
        
    }
}
