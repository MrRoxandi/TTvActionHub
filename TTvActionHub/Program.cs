using System.Runtime.CompilerServices;
using TTvActionHub.Services;
using TTvActionHub.Twitch;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Audio;
using TTvActionHub.Services.Http;
using TTvActionHub.LuaTools.Stuff;
using Microsoft.Extensions.DependencyInjection;

namespace TTvActionHub
{
    internal abstract class Program
    {
        static string path = "config.lua";
        static ServiceProvider? provider;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                path = args[0];
            }

            // Block of code, where we are going to process user command input while programm works
            if (!CheckConfigExists(path))
            {
                var fullpath = @$"{Directory.GetCurrentDirectory()}\{path}";
                Logger.Warn($"Cannot find config in {path}.");
                Logger.Info($"Config will be generated at {fullpath}. Relaunch programm after that");
                CreateConfig(fullpath); // Imagine it is static method of IConfig
                _ = Console.ReadLine();
                return;
            }
            ServiceCollection collection = new();
                collection.AddSingleton<IConfig, Configuration>((o) => new Configuration(path));
                collection.AddSingleton<AudioService>();
                collection.AddSingleton<CommandsService>();
                collection.AddSingleton<RewardsService>();
                collection.AddSingleton<TActionsService>();
                collection.AddSingleton<ContainerService>();
            provider = collection.BuildServiceProvider();

            try
            {
                RunServices();
            }
            catch (Exception ex)
            {
                Logger.Error("While running services, occured an error: ", ex.Message);
                return;
            }

            _ = Console.ReadLine();
            StopServices();

            Logger.Info("Proggram is stopped");
            _ = Console.ReadLine();
        }

        static bool CheckConfigExists(string path) => File.Exists(path);

        private static void RunServices()
        {
            var _commandservice = provider?.GetService<CommandsService>();
           _commandservice?.Run();

            var _rewardsservice = provider?.GetService<RewardsService>();
            _rewardsservice?.Run();

            var _container = provider?.GetService<ContainerService>();
            Storage._service = _container;
            _container?.Run();

            var actionsService = provider?.GetService<TActionsService>();
            actionsService?.Run();

            var _audio = provider?.GetService<AudioService>();
            Sounds.audio = _audio;
            _audio?.Run();

        }

        private static void StopServices()
        {
            var _audio = provider?.GetService<AudioService>();
            _audio?.Stop();

            var _commandservice = provider?.GetService<CommandsService>();
            _commandservice?.Stop();

            var _rewardsservice = provider?.GetService<RewardsService>();
            _rewardsservice?.Stop();

            var _container = provider?.GetService<ContainerService>();
            _container?.Stop();

            var actionsService = provider?.GetService<TActionsService>();
            actionsService?.Stop();

            
        }

        private static void CreateConfig(string path) => File.WriteAllText(path,
@"
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds

local Storage = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Storage
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs
local Users = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Users
local Chat = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Chat

local res = {}
res[""force-relog""] = false -- may be changed to relogin with new account by force 
res[""timeout""] = 1000 -- may be changed
res[""logs""] = false -- may be changed

--res[""opening-bracket""] = '<' -- uncomment if you like !command <arg> more than !command (arg)
--res[""closing-bracket""] = '>' -- bracket may be any symbol, but to work they must be not identical

local commands = {}
local rewards = {}
local tactions = {}

res[""commands""] = commands
res[""rewards""] = rewards
res[""tactions""] = tactions
return res"
                        );
    }
}
