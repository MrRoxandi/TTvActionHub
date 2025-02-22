using System;
using System.Diagnostics;
using System.IO;
using TwitchController.Twitch;
using TwitchController.Logger;

namespace TwitchController
{
    internal abstract class Program
    {
        static void Main(string[] args)
        {

            //var path = @"config.lua";
            var path = @"H:\repos\TwitchController\TwitchController\config.lua";
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

            ConsoleLogger.Info("To close the program press enter.");
            
            var commandService = new TwitchCommandService(configuration);
            commandService.Run();

            var rewardService = new TwitchRewardService(configuration);
            rewardService.Run();

            Console.ReadLine();
        }
    }
}
