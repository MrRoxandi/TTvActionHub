using System.Runtime.CompilerServices;
using TwitchController.Services;
using TwitchController.Twitch;
using TwitchController.Logs;
using TwitchController.LuaTools.Audio;
using TwitchController.Services.Http;

namespace TwitchController
{
    internal abstract class Program
    {
        static List<IService> _services = [];
        static Configuration? _configuration;
        static string path = "config.lua";
        
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
                Configuration.GenerateConfig(fullpath);
                _ = Console.ReadLine();
                return;
            }

            try
            {
                _configuration = new(path);
            }
            catch (Exception ex)
            {
                Logger.Error("While reading configuration occured an error:", ex.Message);
                Logger.Info("Closing program.");
                _ = Console.ReadLine();
                return;
            }
            // Creating services
            if (!CreateAllServices())
            {
                return;
            }
            Logger.Info("All services created. Starting them up");
            
            RunServices();

            _ = Console.ReadLine();

            StopServices();

            Logger.Info("Proggram is stopped");
            
            _ = Console.ReadLine();
        }

        static bool CheckConfigExists(string path) => File.Exists(path);

        private static bool CreateAllServices()
        {
            var _audio = InitService<AudioService>("audio service");
            if (_audio == null) return false;
            Sounds.audio = _audio;
            _services.Add(_audio);

            var _commandservice = InitService<CommandsService>("command service", [_configuration, null]);
            if (_commandservice == null) return false;
            _services.Add(_commandservice);

            var _rewardservice = InitService<RewardsService>("reward service", [_configuration]);
            if (_rewardservice == null) return false;
            _services.Add(_rewardservice);

            return true;
        }

        private static T? InitService<T>(string serviceName, object?[]? args = null) where T : class
        {
            try
            {
                Logger.Info($"Creating {serviceName}");
                if (args == null)
                {
                    T? service = Activator.CreateInstance(typeof(T)) as T;
                    return service;
                }
                else
                {
                    T? service = Activator.CreateInstance(typeof(T), args) as T;
                    return service;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"While creating {serviceName} occurred an error:", ex.Message);
                Logger.Info("Closing program.");
                _ = Console.ReadLine();
                return default;
            }
        }

        private static void RunServices()
        {
            foreach (var service in _services)
                service.Run();
        }

        private static void StopServices()
        {
            foreach(var service in _services)
                service.Stop();
        }

    }
}
