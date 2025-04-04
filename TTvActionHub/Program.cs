using TTvActionHub.Services;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Audio;
using TTvActionHub.LuaTools.Stuff;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Terminal.Gui;

namespace TTvActionHub
{
    internal abstract class Program
    {
        static string ConfigurationPath => Path.Combine(Directory.GetCurrentDirectory(), "configs");
        static ServiceProvider? provider;

        static readonly ConcurrentDictionary<string, IService> runningServices = [];
        static readonly object serviceManagementLock = new();
        static Shell? shell;

        static void Main(string[] args)
        {
            //Application.Init();

            Logger.Info("Application starting...");
            if (!LuaConfigManager.CheckConfiguration())
            {
                Logger.Warn($"Cannot find config in {ConfigurationPath}. Generating...");
                try
                {
                    LuaConfigManager.GenerateConfigs();
                    Logger.Info($"Configuration generated. Please review the files and restart the program.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to generate configuration:", ex);
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
                collection.AddSingleton<IConfig, Configuration>(sp => new Configuration(ConfigurationPath));

                collection.AddSingleton<IService, TwitchChatService>();
                //collection.AddSingleton<IService, EventSubService>();     
                collection.AddSingleton<IService, TimerActionsService>();
                //collection.AddSingleton<IService, ContainerService>();    
                collection.AddSingleton<IService, AudioService>();
                // ---------------------------------------------------------

                // Registrating Shell

                collection.AddSingleton<Shell>(sp =>
                {
                    var config = sp.GetRequiredService<IConfig>();
                    return new Shell(config, RestartServiceByName, RestartAllRunningServices, sp);
                });

                provider = collection.BuildServiceProvider();
                Logger.Info("Dependency Injection configured.");
            }
            catch (Exception ex)
            {
                Application.Shutdown();
                Logger.Error("FATAL: Failed to configure Dependency Injection:", ex);
                return;
            }

            shell = provider.GetService<Shell>();
            if (shell == null)
            {
                Application.Shutdown();
                Logger.Error("FATAL: Unable to initialize Shell.");
                return;
            }
            IDisposable? shellDisposable = shell as IDisposable;
            try
            {
                shell.InitializeUI();

                Logger.Info("Starting services...");
                StartServices(); 
                InitStaticExternLibs();
                Logger.Info("Service startup process finished.");

                // --- Main loop (Terminal.Gui) ---
                Logger.Info("Starting interactive shell UI...");
                shell.Run(); 
                Logger.Info("Shell UI exited.");
            }
            catch (Exception ex)
            {
                Logger.Error("A critical error occurred during service startup or shell execution:", ex);
            }
            finally
            {
                Logger.Info("Stopping services...");
                StopServices();
                Logger.Info("Service shutdown process finished.");

                shellDisposable?.Dispose(); 
                Application.Shutdown(); 
                
                Logger.Info("Program finished.");
                               
            }
        }

        // --- SERVICE MANAGEMENT METHODS ---

        private static void StartServices()
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

                // Thread safe check and insertion
                if(runningServices.TryAdd(serviceName, service))
                {
                    shell.AddService(serviceName);
                    shell.CmdOut($"Attemting to start {serviceName}...");
                    Logger.Info($"Attemting to start {serviceName}...");

                    try
                    {
                        service.StatusChanged += OnServiceStatusChangedHandler;
                        service.Run();
                        bool currentStatus = service.IsRunning; 
                        shell.UpdateServicesStates(serviceName, currentStatus); 
                        if (currentStatus)
                        {
                            shell.CmdOut($"{serviceName} run command issued, service reports running.");
                            Logger.Info($"{serviceName} run command issued, service reports running.");
                        }
                        else
                        {
                            shell.CmdOut($"{serviceName} run command issued, but service reports not running immediately.");
                            Logger.Warn($"{serviceName} run command issued, but service reports not running immediately.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to start {serviceName}:", ex);
                        shell.CmdOut($"ERROR starting {serviceName}: {ex.Message}");
                        shell.UpdateServicesStates(serviceName, false);
                        service.StatusChanged -= OnServiceStatusChangedHandler;
                        runningServices.TryRemove(serviceName, out _);
                    }
                } else
                {
                    Logger.Warn($"Service {serviceName} was already in running list during startup. Skipping duplicate start.");
                    if (runningServices.TryGetValue(serviceName, out var existingService))
                    {
                        shell.UpdateServicesStates(serviceName, existingService.IsRunning);
                    }
                }
            }
        }

        private static void StopServices()
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

        private static void RestartServiceByName(string serviceName)
        {
            if (shell == null || provider == null) return;
            // Needed to be locked for hard work (:/)
            lock (serviceManagementLock)
            {
                shell.CmdOut($"Attempting to restart service: {serviceName}...");
                Logger.Info($"Restart requested for service: {serviceName}");
                IService? serviceInstance;
                bool wasRunning = runningServices.TryGetValue(serviceName, out serviceInstance);

                if (serviceInstance == null && !wasRunning)
                {
                    // Trying find in DI if service was not running at all
                    serviceInstance = provider.GetServices<IService>()
                        .FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                    if (serviceInstance == null)
                    {
                        shell.CmdOut($"ERROR: Service '{serviceName}' not found in registry.");
                        Logger.Error($"Cannot restart: Service '{serviceName}' not found in DI container.");
                        return;
                    }
                    Logger.Info($"Service '{serviceName}' was not running. Will attempt to start.");
                }
                else if (serviceInstance == null && wasRunning) 
                {
                    // VERY STRANGE SITUATION ???
                    Logger.Error($"Inconsistent state for service '{serviceName}' during restart. Aborting.");
                    shell.CmdOut($"ERROR: Inconsistent state for {serviceName}. Restart aborted.");
                    
                    runningServices.TryRemove(serviceName, out _);
                    return;
                }

                if (wasRunning && serviceInstance != null)
                {
                    shell.CmdOut($"Stopping {serviceName}...");
                    Logger.Info($"Stopping {serviceName} for restart...");
                    try
                    {
                        serviceInstance.StatusChanged -= OnServiceStatusChangedHandler;
                        serviceInstance.Stop();

                        if (!runningServices.TryRemove(serviceName, out _))
                        {
                            Logger.Warn($"Service {serviceName} was not found in running list during removal for restart.");
                        }
                        shell.UpdateServicesStates(serviceName, false);
                        shell.CmdOut($"{serviceName} stopped.");
                        Logger.Info($"{serviceName} stopped successfully.");
                    }
                    catch (Exception ex)
                    {
                        shell.UpdateServicesStates(serviceName, false);
                        shell.CmdOut($"ERROR stopping {serviceName}: {ex.Message}. Restart aborted.");
                        Logger.Error($"Failed to stop {serviceName} during restart:", ex);
                        runningServices.TryRemove(serviceName, out _);
                    }
                }

                Thread.Sleep(20); // Smol break to drink coffe

                // --- Starting up serivice ---

                if (serviceInstance == null)
                {
                    Logger.Error($"Cannot start service '{serviceName}' during restart: instance is null.");
                    shell.CmdOut($"ERROR: Could not obtain instance for {serviceName}. Start aborted.");
                    return;
                }

                shell.CmdOut($"Starting {serviceName}...");
                Logger.Info($"Starting {serviceName} as part of restart...");
                try
                {
                    runningServices.TryAdd(serviceName, serviceInstance);
                    serviceInstance.StatusChanged += OnServiceStatusChangedHandler;
                    serviceInstance.Run();

                    bool currentStatusRestart = serviceInstance.IsRunning;
                    shell.UpdateServicesStates(serviceName, currentStatusRestart);
                    if (currentStatusRestart)
                    {
                        shell.CmdOut($"{serviceName} started successfully."); 
                        Logger.Info($"{serviceName} started successfully after restart request.");
                    }
                    else
                    {
                        shell.CmdOut($"WARNING: {serviceName} started but reported not running immediately after restart.");
                        Logger.Warn($"{serviceName} reports not running immediately after restart Run().");
                    }
                }
                catch (Exception ex)
                {
                    shell.UpdateServicesStates(serviceName, false);
                    shell.CmdOut($"ERROR starting {serviceName}: {ex.Message}");
                    Logger.Error($"Failed to start {serviceName} during restart:", ex);

                    serviceInstance.StatusChanged -= OnServiceStatusChangedHandler;
                    runningServices.TryRemove(serviceName, out _);
                }
            }
        }

        private static void RestartAllRunningServices()
        {
            throw new NotImplementedException();
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

        public static void InitStaticExternLibs()
        {
            runningServices.TryGetValue("TwitchChat", out var tcServ);
            if(tcServ is not TwitchChatService tcs) 
            {
                throw new NullReferenceException(nameof(TwitchChatService));
            }
            {
                TwitchChat.Client = tcs.Client;
                TwitchChat.Channel = tcs.Channel;
            }
            runningServices.TryGetValue("AudioService", out var auServ);
            if(auServ is not AudioService aus)
            {
                throw new NullReferenceException(nameof(AudioService));
            }
            {
                Sounds.audio = aus;
            }
        }
    }
}
