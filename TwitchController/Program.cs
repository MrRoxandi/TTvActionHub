using System.Runtime.CompilerServices;
using TwitchController.Services;
using TwitchController.Twitch;
using TwitchController.Logger;

namespace TwitchController
{
    internal abstract class Program
    {
        static IEnumerable<IService> Services;

        static void Main(string[] args)
        {
            var path = @"config.lua";
            if (!File.Exists(path) && args.Length == 0){
                var fullPath = Directory.GetCurrentDirectory() + "\\" + path;
                ConsoleLogger.Error($"Cannot find {path} in main directory.");
                ConsoleLogger.Info($"It will be generated at {fullPath}.");
                
                Configuration.GenerateConfig(fullPath);
                Console.ReadLine();
                return;
            }

            if (args.Length > 0) path = args[0];

            if (!File.Exists(path))
            {
                ConsoleLogger.Error($"Cannot find config.lua file in {path}");
                Console.ReadLine();
                return;
            }

            Configuration configuration;
            try { configuration = new Configuration(path); }
            catch (Exception ex)
            {
                ConsoleLogger.Error($"While creating configuration occured an error: {ex.Message}.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("[INFO] PRESS ENTER TO CLOSE PROGRAM");

            RegisterServices([
                new TwitchCommandService(configuration),
                new TwitchRewardService(configuration),
                new Services.Http.Service("http://localhost", "8888"),
            ]);
            
            Console.ReadLine();

            StopServices();
        }

        private static void RegisterServices(IEnumerable<IService> services)
        {
            Services = services;
            foreach (var service in services)
                service.Run();
        }

        private static void StopServices()
        {
            foreach(var service in Services)
                service.Stop();
        }
    }
}
