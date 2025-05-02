using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using TTvActionHub.LuaTools.Audio;
using TTvActionHub.LuaTools.Stuff;
using TTvActionHub.Services;
using TTvActionHub.Managers;
using TTvActionHub.Logs;
using Terminal.Gui;

namespace TTvActionHub
{
    internal abstract class Program
    {
        static string ConfigurationPath => Path.Combine(Directory.GetCurrentDirectory(), "configs");
        static ServiceProvider? provider;

        static readonly ConcurrentDictionary<string, IService> runningServices = [];

        static readonly object serviceManagementLock = new();
        static LuaConfigManager? luaConfigManager;
        static Shell? shell;

        static void Main(/*string[] args*/)
        {
            //Application.Init();

            Logger.Info("Application starting...");
            if (!LuaConfigManager.CheckConfiguration())
            {
                Logger.Warn($"Cannot find all configs in {ConfigurationPath}. Generating...");
                Console.WriteLine($"Cannot find all configs in {ConfigurationPath}. Generating...");
                try
                {
                    LuaConfigManager.GenerateAllConfigs();
                    Logger.Info($"Configurations generated. Please review the files and restart the program.");
                    Console.WriteLine($"Configurations generated. Please review the files and restart the program.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to generate configurations:", ex);
                    Console.WriteLine($"Failed to generate configurations: {ex.Message}");
                }
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
                return;
            }

            Logger.Info("Configuration found.");

            // Configuring DI 
            try
            {
                ServiceCollection collection = new();
                collection.AddSingleton<LuaConfigManager>();
                collection.AddSingleton<IConfig, Configuration>(sp =>
                {
                    var lcm = sp.GetRequiredService<LuaConfigManager>();
                    return new Configuration(lcm);
                });

                collection.AddSingleton<IService, TwitchChatService>();
                collection.AddSingleton<IService, EventSubService>();
                collection.AddSingleton<IService, TimerActionsService>();
                collection.AddSingleton<IService, ContainerService>();
                collection.AddSingleton<IService, AudioService>();
                // ---------------------------------------------------------

                // Registrating Shell

                collection.AddSingleton<Shell>(sp =>
                {
                    var config = sp.GetRequiredService<IConfig>();
                    return new Shell(config, 
                        StartServiceByName, StopServiceByName, ReloadServiceConfiguraionByName,
                        GetServiceInfoByName);
                });

                provider = collection.BuildServiceProvider();
                Logger.Info("Dependency Injection configured.");
                Console.WriteLine("Dependency Injection configured.");
            }
            catch (Exception ex)
            {
                Application.Shutdown();
                Logger.Error("FATAL: Failed to configure Dependency Injection:", ex);
                Console.WriteLine($"FATAL: Failed to configure Dependency Injection: {ex.Message}");
                _ = Console.ReadLine();
                return;
            }
            shell = provider.GetService<Shell>();
            if (shell == null)
            {
                Application.Shutdown();
                Logger.Error("FATAL: Unable to initialize Shell.");
                Console.WriteLine("FATAL: Unable to initialize Shell.");
                _ = Console.ReadLine();
                return;
            }

            //Shell shellDisposable = shell;
            try
            {
                Console.WriteLine("Initializing Shell UI...");
                Logger.Info("Initializing Shell UI...");
                shell.InitializeUI();

                Console.WriteLine("Initializ services...");
                Logger.Info("Initializ services...");

                InitAllServices();

                Console.WriteLine("Initializ static lua bridges...");
                Logger.Info("Initializ static lua bridges...");

                InitializeStaticLuaBridges();
                
                // --- Main loop (Terminal.Gui) ---
                Logger.Info("Starting interactive shell UI...");
                shell.Run();
                //Task.Run(shell.Run);

                Logger.Info("Shell UI exited.");
            }
            catch (Exception ex)
            {
                Logger.Error("A critical error occurred during service startup or shell execution:", ex);
            }
            finally
            {
                Logger.Info("Stopping services...");
                DeInitAllServices();
                Logger.Info("Service shutdown process finished.");

                shell?.Dispose(); 
                Application.Shutdown(); 
                
                Logger.Info("Program finished.");
                               
            }
        }

        // --- SERVICE MANAGEMENT METHODS ---

        // --- First run for all services. Calls only once ---

        private static void InitAllServices()
        {
            if (provider == null || shell == null)
            {
                Logger.Warn("Cannot start services: Provider or Shell is not initialized.");
                return;
            }

            var servicesToStart = provider.GetServices<IService>();

            foreach (var service in servicesToStart)
            {
                var serviceName = service.ServiceName;
                if (string.IsNullOrEmpty(serviceName))
                {
                    Logger.Warn($"Service of type {service.GetType().Name} has missing ServiceName. Skipping.");
                    continue;
                }
                shell.AddService(serviceName);
                shell.CmdOut($"Attemting to start {serviceName}...");
                Logger.Info($"Attemting to start {serviceName}...");
                try
                {
                    service.StatusChanged += OnServiceStatusChangedHandler;
                    service.Run();
                    bool state = service.IsRunning;
                    shell.UpdateServicesStates(serviceName, state);
                    if (state)
                    {
                        shell.CmdOut($"{serviceName} run command issued, service reports running.");
                        Logger.Info($"{serviceName} run command issued, service reports running.");
                    }
                    else
                    {
                        shell.CmdOut($"{serviceName} run command issued, but service reports not running immediately.");
                        Logger.Warn($"{serviceName} run command issued, but service reports not running immediately.");
                    }
                    runningServices.TryAdd(serviceName, service);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to start {serviceName}:", ex);
                    shell.CmdOut($"Error starting {serviceName}: {ex.Message}");
                    shell.UpdateServicesStates(serviceName, false);
                    service.StatusChanged -= OnServiceStatusChangedHandler;
                }
            }
        }

        // --- Last stop for all services. Calls only once ---

        private static void DeInitAllServices()
        {
            if (provider == null || shell == null)
            {
                Logger.Warn("Cannot stop services: Provider or Shell is not initialized.");
                return;
            }
            shell.CmdOut("Attempting to stop all running services...");
            var serviceNamesToStop = runningServices.Keys.ToList();
            foreach (var sname in serviceNamesToStop)
            {
                if (runningServices.TryRemove(sname, out var service))
                {
                    shell.CmdOut($"Attempting to stop {sname}...");
                    Logger.Info($"Attempting to stop {sname}...");
                    try
                    {
                        service.StatusChanged -= OnServiceStatusChangedHandler;
                        service.Stop();
                        shell.UpdateServicesStates(sname, false);
                        shell.CmdOut($"{sname} stopped.");
                        Logger.Info($"{sname} stopped successfully.");
                    }
                    catch (Exception ex)
                    {
                        shell.UpdateServicesStates(sname, false); // Anyway it is not working...
                        shell.CmdOut($"ERROR stopping {sname}: {ex.Message}");
                        Logger.Error($"Failed to stop {sname}:", ex);
                    }
                }
            }
            runningServices.Clear();
            shell.CmdOut("Service shutdown process finished.");
        }

        private static string[]? GetServiceInfoByName(string name)
        {
            if (provider == null || shell == null)
            {
                Logger.Error("Get service info: Provider or Shell is not initialized.");
                return null;
            }

            var finded = runningServices.TryGetValue(name, out var service);
            if(!finded || service == null)
            {
                shell?.CmdOut($"Unable to find running service with name: [{name}] to get it's information");
                return null;
            }

            if (service is TwitchChatService tcs) return tcs.Commands?.Keys.ToArray() ?? [];
            if (service is EventSubService ess) return ess.Rewards?.Keys.ToArray() ?? [];
            if (service is TimerActionsService tas) return tas.TActions?.Keys.ToArray() ?? [];
            return [];
        }

        private static void ReloadServiceConfiguraionByName(string name)
        {
            if (provider == null || shell == null)
            {
                Logger.Error("Cannot reload service configuration: Provider or Shell is not initialized.");
                return;
            }

            var finded = runningServices.TryGetValue(name, out var service);
            
            if(!finded || service == null)
            {
                shell?.CmdOut($"Unable to find running service with name: [{name}] to update it's configuration");
                return;
            }

            if (service is not IUpdatableConfiguration updatableService)
            {
                shell?.CmdOut($"[{name}] is not service that can update it's configuration");
                return;
            }
            var result = updatableService.UpdateConfiguration();
            if (!result)
            {
                shell.CmdOut($"Failed to update configuration for [{name}]. Check logs for more info...");
                return;
            }

            shell.CmdOut($"Configuration was updated for [{name}].");
        }

        private static void StopServiceByName(string name)
        {
            if (provider == null || shell == null)
            {
                Logger.Error("Cannot stop services: Provider or Shell is not initialized.");
                return;
            }
            var finded = runningServices.TryRemove(name, out var service);
            if (service == null || !finded)
            {
                shell.CmdOut($"Unable to get service with name: {name}");
                Logger.Error($"Unable to get service with name: {name}");
                return;
            }
            else
            {
                shell.CmdOut($"Attempting to stop {service.ServiceName}...");
                Logger.Info($"Attempting to stop {service.ServiceName}...");
                try
                {
                    service.StatusChanged -= OnServiceStatusChangedHandler;
                    service.Stop();
                    shell.UpdateServicesStates(service.ServiceName, false);
                    shell.CmdOut($"{service.ServiceName} has stopped");
                    Logger.Info($"{service.ServiceName} has stopped");
                }
                catch (Exception ex)
                {
                    shell.UpdateServicesStates(name, false); // Anyway we get ex during stopping :/
                    shell.CmdOut($"Error during stopping service {name}.");
                    Logger.Error($"Error during stopping service {name}.", ex);
                }
            }
        }

        private static void StartServiceByName(string name)
        {
            if (shell == null || provider == null)
            {
                Logger.Warn("Cannot start services: Provider or Shell is not initialized.");
                return;
            }

            var finded = runningServices.TryGetValue(name, out var service);
            if (finded && service != null)
            {
                if (service.IsRunning)
                {
                    shell.CmdOut($"Service {name} already running");
                    Logger.Warn($"Service {name} already running");
                    return;
                }
                runningServices.TryRemove(service.ServiceName, out _);
            }
            // --- Attempting to find service ---
            service = provider.GetServices<IService>().FirstOrDefault(
                sv => sv.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (service == default(IService))
            {
                shell.CmdOut($"Unable to start service: {name}. Looks like it dosn't registred");
                Logger.Warn($"Unable to start service: {name}. Looks like it dosn't registred");
                return;
            }
            shell.CmdOut($"Attempting to start service: {name}...");
            Logger.Warn($"Unable to start service: {name}...");
            try
            {
                service.StatusChanged += OnServiceStatusChangedHandler;
                // service.Run()
                Task.Run(service.Run);
                var isrunning = service.IsRunning;
                shell.UpdateServicesStates(service.ServiceName, isrunning);
                if (isrunning)
                {
                    shell.CmdOut($"{service.ServiceName} run command issued, service reports running.");
                    Logger.Info($"{service.ServiceName} run command issued, service reports running.");
                }
                else
                {
                    shell.CmdOut($"{service.ServiceName} run command issued, but service reports not running immediately.");
                    Logger.Warn($"{service.ServiceName} run command issued, but service reports not running immediately.");
                }
                runningServices.TryAdd(service.ServiceName, service);
                UpdateStaticLuaBridges(service);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start {service.ServiceName}:", ex);
                shell.CmdOut($"Error starting {service.ServiceName}: {ex.Message}");
                shell.UpdateServicesStates(service.ServiceName, false);
                service.StatusChanged -= OnServiceStatusChangedHandler;
            }
        }

        // --- SERVICE STATUS EVENT HANDLER ---
        private static void OnServiceStatusChangedHandler(object? sender, ServiceStatusEventArgs e)
        {
            if (shell == null || sender == null) return;

            shell.UpdateServicesStates(e.ServiceName, e.IsRunning);

            string reason = string.IsNullOrEmpty(e.Message) ? "" : $" Reason: {e.Message}";
            bool wasExpectedRunning = runningServices.ContainsKey(e.ServiceName);

            if (!e.IsRunning && wasExpectedRunning)
            {
                Logger.Warn($"Service '{e.ServiceName}' stopped unexpectedly.{reason}");
                shell.CmdOut($"ALERT: Service '{e.ServiceName}' stopped!{reason}");

                if (runningServices.TryRemove(e.ServiceName, out _))
                {
                    Logger.Info($"Service '{e.ServiceName}' removed from active list due to unexpected stop.");
                }
            }

            else if (e.IsRunning && !wasExpectedRunning)
            {
                Logger.Warn($"Service '{e.ServiceName}' reported running unexpectedly.{reason}");
                if (sender is IService serviceInstance)
                {
                    runningServices.TryAdd(e.ServiceName, serviceInstance);
                }
            } else
            {
                Logger.Info($"Service '{e.ServiceName}' status changed to: {(e.IsRunning ? "Running" : "Stopped")}.{reason}");
            }
        }

        private static void InitializeStaticLuaBridges()
        {
            try
            {
                Logger.Info("Initializing static bridges for Lua...");

                var allServices = provider!.GetServices<IService>();
                foreach (var service in allServices)
                {
                    if (service is TwitchChatService ttvServ)
                    {
                        TwitchChat.Client = ttvServ!.Client;
                        TwitchChat.Channel = ttvServ!.Channel;
                    }
                    else if (service is AudioService audioServ)
                        Sounds.audio = audioServ!;
                    else if (service is ContainerService containerServ)
                        Storage.Container = containerServ!;
                }
            } catch (Exception ex)
            {
                Logger.Error("FATAL: Failed to initialize static Lua bridges:", ex);
                throw new InvalidOperationException("Failed to initialize static Lua bridges. See logs for details.", ex);
            }
        }

        private static void UpdateStaticLuaBridges(IService s)
        {
            shell!.CmdOut($"Updating static bridges for {s.ServiceName}...");
            Logger.Info($"Updating static bridges for {s.ServiceName}...");

            if (s is TwitchChatService ttvServ)
            {
                TwitchChat.Client = ttvServ!.Client;
                TwitchChat.Channel = ttvServ!.Channel;
            }
            else if (s is AudioService audioServ)
                Sounds.audio = audioServ!;
            else if (s is ContainerService containerServ)
                Storage.Container = containerServ!;
            Logger.Info("Static bridges updated successfully.");
        }
    }
}
