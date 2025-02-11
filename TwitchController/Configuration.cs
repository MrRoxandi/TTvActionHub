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
        private static string ClientId { get => "client id"; }
        private static string ClientSecret { get => "client secret"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public readonly TwitchApiService TwitchApi;
        public readonly string TwitchChannel;
        public readonly string TwitchID;
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

            if (luaConfig["channel"] is not string channel)
            {
                throw new Exception($"Unable to get channel from file: {path}");
            }
            TwitchChannel = channel;
            
            TwitchApi = new TwitchApiService(ClientId, ClientSecret, RedirectUrl);

            TwitchID = TwitchApi.GetTwitchIdAsync(TwitchChannel).Result;
            if (TokenManager.LoadToken(TwitchID) is not string token)
            {
                var tmp = TwitchApi.RunAuthFlowAsync().Result;
                if (string.IsNullOrEmpty(tmp))
                {
                    throw new Exception($"Failed to get token for channel: {TwitchChannel}");
                }
                Token = tmp;
            }
            else Token = token;

            TokenManager.SaveToken(TwitchID, Token);

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
        
    }
}
