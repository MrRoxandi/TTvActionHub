using TTvActionHub.Services;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Audio;
using TTvActionHub.LuaTools.Stuff;
using Microsoft.Extensions.DependencyInjection;

namespace TTvActionHub
{
    internal abstract class Program
    {
        static string ConfigurationPath => Path.Combine(Directory.GetCurrentDirectory(), "configs");
        static ServiceProvider? provider;

        static void Main(string[] args)
        {
            // Block of code, where we are going to process user command input while programm works
            if (!LuaConfigManager.CheckConfiguration())
            {
                Logger.Warn($"Cannot find config in {ConfigurationPath}.");
                Logger.Info($"All configuration files will be generated at {ConfigurationPath}");
                Logger.Info($"Starting process...");
                LuaConfigManager.GenerateConfigs();
                Logger.Info($"All done. Restart the program.");
                _ = Console.ReadLine();
                return;
            }
            ServiceCollection collection = new();
            collection.AddSingleton<IConfig, Configuration>((o) => new Configuration(ConfigurationPath));
            collection.AddSingleton<AudioService>();
            collection.AddSingleton<TwitchChatService>();
            collection.AddSingleton<EventSubService>();
            collection.AddSingleton<TimerActionsService>();
            collection.AddSingleton<ContainerService>();
            provider = collection.BuildServiceProvider();

            try
            {
                RunServices();
            }
            catch (Exception ex)
            {
                Logger.Error("While running services, occured an error: ", ex);
                _ = Console.ReadLine();
                return;
            }

            _ = Console.ReadLine();
            StopServices();

            Logger.Info("Proggram is stopped");
            _ = Console.ReadLine();
        }

        private static void RunServices()
        {
            var _chatservice = provider?.GetService<TwitchChatService>();
            TwitchChat.Client = _chatservice!.Client;
            TwitchChat.Channel = _chatservice!.Channel;
            _chatservice!.Run();

            var _eventsubservice = provider?.GetService<EventSubService>();
            _eventsubservice!.Run();

            var _container = provider?.GetService<ContainerService>();
            Storage._service = _container;
            _container!.Run();

            var actionsService = provider?.GetService<TimerActionsService>();
            actionsService!.Run();

            var _audio = provider?.GetService<AudioService>();
            Sounds.audio = _audio;
            _audio!.Run();

        }

        private static void StopServices()
        {
            var _audio = provider?.GetService<AudioService>();
            _audio?.Stop();

            var _commandservice = provider?.GetService<TwitchChatService>();
            _commandservice?.Stop();

            var _rewardsservice = provider?.GetService<EventSubService>();
            _rewardsservice?.Stop();

            var _container = provider?.GetService<ContainerService>();
            _container?.Stop();

            var actionsService = provider?.GetService<TimerActionsService>();
            actionsService?.Stop();

        }
    }
}
