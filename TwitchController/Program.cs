using System;
using System.Diagnostics;
using System.IO;
using TwitchController.Twitch;

namespace TwitchController
{
    internal abstract class Program
    {
        static void Main(string[] args)
        {
            var path = @"F:\Repos\TwitchController\TwitchController\config.lua";
            //var path = @"config.lua";
            if (!File.Exists(path) && args.Length == 0){
                Console.WriteLine($"Cannot find {path} in main directory");
                Console.WriteLine($"Specify input .lua file path as the first argument");
                return;
            } 

            if (args.Length > 0) path = args[0];

            if (!File.Exists(path))
            {
                Console.WriteLine($"Cannot find lua config file in {path}");
                return;
            }

            Configuration configuration;
            try { configuration = new Configuration(path); }
            catch (Exception e)
            {
                Console.WriteLine($"[ERR] {e.Message}");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("[INFO] PRESS ENTER TO CLOSE PROGRAM");
            
            var commandService = new TwitchCommandService(configuration);
            commandService.Run();

            var rewardService = new TwitchRewardService(configuration);
            rewardService.Run();

            Console.ReadLine();
        }
    }
}
