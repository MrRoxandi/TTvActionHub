using System.Runtime.CompilerServices;
using TTvActionHub.Services;
using TTvActionHub.Twitch;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Audio;
using TTvActionHub.Services.Http;
using TTvActionHub.LuaTools.Stuff;

namespace TTvActionHub
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

            // Command processing loop
            string? command;
            do
            {
                Console.Write("Enter command (reload, exit): ");
                command = Console.ReadLine()?.ToLower();

                switch (command)
                {
                    case "reload":
                        ReloadConfiguration();
                        break;
                    case "exit":
                        break;
                    default:
                        Logger.Warn("Unknown command.");
                        break;
                }
            } while (command != "exit");

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

            var _containerservice = InitService<ContainerService>("container service");
            if (_containerservice == null) return false;
            Storage._service = _containerservice;
            _services.Add(_containerservice);

            var _teventservice = InitService<TEventsService>("timer events service", [_configuration]);
            if (_teventservice == null) return false;
            _services.Add(_teventservice);

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
            foreach (var service in _services)
                service.Stop();
        }

        static void ReloadConfiguration()
        {
            Logger.Info("Reloading configuration...");

            // 1. Stop existing services
            StopServices();
            _services.Clear();  // Very important:  Clear the list!

            // 2. Reload configuration
            try
            {
                _configuration = new(path); // Re-create the configuration
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading configuration:", ex.Message);
                return;
            }

            // 3. Re-create services
            if (!CreateAllServices())
            {
                return;
            }

            // 4. Start services again
            Logger.Info("Starting services after reload...");
            RunServices();
            Logger.Info("Configuration reloaded successfully.");
        }

    }
}