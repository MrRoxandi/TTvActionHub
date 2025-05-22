using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using TTvActionHub.BackEnds;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services;
using TTvActionHub.Managers;
using TTvActionHub.Services;

namespace TTvActionHub;

internal abstract class Program
{
    private static string ConfigurationPath => Path.Combine(Directory.GetCurrentDirectory(), "configs");
    private static ServiceProvider? _provider;

    private static readonly ConcurrentDictionary<string, IService> RunningServices = [];

    private static Shell? _shell;

    private static void Main( /*string[] args*/)
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
                Logger.Info("Configurations generated. Please review the files and restart the program.");
                Console.WriteLine("Configurations generated. Please review the files and restart the program.");
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
            collection.AddSingleton<DataContainer>();

            collection.AddSingleton<LuaConfigManager>();
            collection.AddSingleton<IConfig, Configuration>(sp =>
            {
                var lcm = sp.GetRequiredService<LuaConfigManager>();
                return new Configuration(lcm);
            });

            collection.AddSingleton<IService, TwitchService>();
            collection.AddSingleton<IService, TimerActionsService>();
            collection.AddSingleton<IService, AudioService>();
            // ---------------------------------------------------------

            // Registration Shell

            collection.AddSingleton<Shell>(sp =>
            {
                var config = sp.GetRequiredService<IConfig>();
                return new Shell(config,
                    StartServiceByName, StopServiceByName, ReloadServiceConfigurationByName,
                    GetServiceInfoByName);
            });

            _provider = collection.BuildServiceProvider();
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

        _shell = _provider.GetService<Shell>();
        Container.Storage = _provider.GetService<DataContainer>();
        if (_shell == null)
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
            _shell.InitializeUi();

            Console.WriteLine("Initializing services...");
            Logger.Info("Initializing services...");

            InitAllServices();

            Console.WriteLine("Initializing static lua bridges...");
            Logger.Info("Initializing static lua bridges...");

            InitializeStaticLuaBridges();

            // --- Main loop (Terminal.Gui) ---
            Logger.Info("Starting interactive shell UI...");
            _shell.Run();
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

            _shell.Dispose();
            Application.Shutdown();

            Logger.Info("Program finished.");
        }
    }

    // --- SERVICE MANAGEMENT METHODS ---

    // --- First run for all services. Calls only once ---

    private static void InitAllServices()
    {
        if (_provider == null || _shell == null)
        {
            Logger.Warn("Cannot start services: Provider or Shell is not initialized.");
            return;
        }

        var servicesToStart = _provider.GetServices<IService>();

        foreach (var service in servicesToStart)
        {
            var serviceName = service.ServiceName;
            if (string.IsNullOrEmpty(serviceName))
            {
                Logger.Warn($"Service of type {service.GetType().Name} has missing ServiceName. Skipping.");
                continue;
            }

            _shell.AddService(serviceName);
            _shell.CmdOut($"Attempting to start {serviceName}...");
            Logger.Info($"Attempting to start {serviceName}...");
            try
            {
                service.StatusChanged += OnServiceStatusChangedHandler;
                service.Run();
                var state = service.IsRunning;
                _shell.UpdateServicesStates(serviceName, state);
                if (state)
                {
                    _shell.CmdOut($"{serviceName} run command issued, service reports running.");
                    Logger.Info($"{serviceName} run command issued, service reports running.");
                }
                else
                {
                    _shell.CmdOut($"{serviceName} run command issued, but service reports not running immediately.");
                    Logger.Warn($"{serviceName} run command issued, but service reports not running immediately.");
                }

                RunningServices.TryAdd(serviceName, service);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start {serviceName}:", ex);
                _shell.CmdOut($"Error starting {serviceName}: {ex.Message}");
                _shell.UpdateServicesStates(serviceName, false);
                service.StatusChanged -= OnServiceStatusChangedHandler;
            }
        }
    }

    // --- Last stop for all services. Calls only once ---

    private static void DeInitAllServices()
    {
        if (_provider == null || _shell == null)
        {
            Logger.Warn("Cannot stop services: Provider or Shell is not initialized.");
            return;
        }

        _shell.CmdOut("Attempting to stop all running services...");
        var serviceNamesToStop = RunningServices.Keys.ToList();
        foreach (var sName in serviceNamesToStop)
        {
            if (!RunningServices.TryRemove(sName, out var service)) continue;
            _shell.CmdOut($"Attempting to stop {sName}...");
            Logger.Info($"Attempting to stop {sName}...");
            try
            {
                service.StatusChanged -= OnServiceStatusChangedHandler;
                service.Stop();
                _shell.UpdateServicesStates(sName, false);
                _shell.CmdOut($"{sName} stopped.");
                Logger.Info($"{sName} stopped successfully.");
            }
            catch (OperationCanceledException)
            {
                _shell.UpdateServicesStates(sName, false);
                _shell.CmdOut($"{sName} stopped.");
                Logger.Info($"{sName} stopped successfully.");
            }
            catch (Exception ex)
            {
                _shell.UpdateServicesStates(sName, false); // Anyway it is not working...
                _shell.CmdOut($"Error stopping {sName}: {ex.Message}");
                Logger.Error($"Failed to stop {sName}:", ex);
            }
        }

        RunningServices.Clear();
        _shell.CmdOut("Service shutdown process finished.");
    }

    private static string[]? GetServiceInfoByName(string name)
    {
        if (_provider == null || _shell == null)
        {
            Logger.Error("Get service info: Provider or Shell is not initialized.");
            return null;
        }

        var finded = RunningServices.TryGetValue(name, out var service);
        if (finded && service != null)
            return service switch
            {
                TwitchService ess => ess.TwitchEvents?.Values.Select(e => e.Name).ToArray() ?? [],
                TimerActionsService tas => tas.Actions?.Keys.ToArray() ?? [],
                _ => []
            };
        _shell.CmdOut($"Unable to find running service with name: [{name}] to get it's information");
        return null;
    }

    private static void ReloadServiceConfigurationByName(string name)
    {
        if (_provider == null || _shell == null)
        {
            Logger.Error("Cannot reload service configuration: Provider or Shell is not initialized.");
            return;
        }

        var finded = RunningServices.TryGetValue(name, out var service);

        if (!finded || service == null)
        {
            _shell.CmdOut($"Unable to find running service with name: [{name}] to update it's configuration");
            return;
        }

        if (service is not IUpdatableConfiguration updatableService)
        {
            _shell.CmdOut($"[{name}] is not service that can update it's configuration");
            return;
        }

        var result = updatableService.UpdateConfiguration();
        if (!result)
        {
            _shell.CmdOut($"Failed to update configuration for [{name}]. Check logs for more info...");
            return;
        }

        _shell.CmdOut($"Configuration was updated for [{name}].");
    }

    private static void StopServiceByName(string name)
    {
        if (_provider == null || _shell == null)
        {
            Logger.Error("Cannot stop services: Provider or Shell is not initialized.");
            return;
        }

        var finded = RunningServices.TryRemove(name, out var service);
        if (service == null || !finded)
        {
            _shell.CmdOut($"Unable to get service with name: {name}");
            Logger.Error($"Unable to get service with name: {name}");
        }
        else
        {
            _shell.CmdOut($"Attempting to stop {service.ServiceName}...");
            Logger.Info($"Attempting to stop {service.ServiceName}...");
            try
            {
                service.StatusChanged -= OnServiceStatusChangedHandler;
                service.Stop();
                _shell.UpdateServicesStates(service.ServiceName, false);
                _shell.CmdOut($"{service.ServiceName} has stopped");
                Logger.Info($"{service.ServiceName} has stopped");
            }
            catch (Exception ex)
            {
                _shell.UpdateServicesStates(name, false); // Anyway we get ex during stopping :/
                _shell.CmdOut($"Error during stopping service {name}.");
                Logger.Error($"Error during stopping service {name}.", ex);
            }
        }
    }

    private static void StartServiceByName(string name)
    {
        if (_shell == null || _provider == null)
        {
            Logger.Warn("Cannot start services: Provider or Shell is not initialized.");
            return;
        }

        var finded = RunningServices.TryGetValue(name, out var service);
        if (finded && service != null)
        {
            if (service.IsRunning)
            {
                _shell.CmdOut($"Service {name} already running");
                Logger.Warn($"Service {name} already running");
                return;
            }

            RunningServices.TryRemove(service.ServiceName, out _);
        }

        // --- Attempting to find service ---
        service = _provider.GetServices<IService>()
            .FirstOrDefault(sv => sv.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (service == null)
        {
            _shell.CmdOut($"Unable to start service: {name}. Looks like it doesn't registered");
            Logger.Warn($"Unable to start service: {name}. Looks like it doesn't registered");
            return;
        }

        _shell.CmdOut($"Attempting to start service: {name}...");
        Logger.Warn($"Unable to start service: {name}...");
        try
        {
            service.StatusChanged += OnServiceStatusChangedHandler;
            // service.Run()
            Task.Run(service.Run);
            var isRunning = service.IsRunning;
            _shell.UpdateServicesStates(service.ServiceName, isRunning);
            if (isRunning)
            {
                _shell.CmdOut($"{service.ServiceName} run command issued, service reports running.");
                Logger.Info($"{service.ServiceName} run command issued, service reports running.");
            }
            else
            {
                _shell.CmdOut(
                    $"{service.ServiceName} run command issued, but service reports not running immediately.");
                Logger.Warn($"{service.ServiceName} run command issued, but service reports not running immediately.");
            }

            RunningServices.TryAdd(service.ServiceName, service);
            UpdateStaticLuaBridges(service);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start {service.ServiceName}:", ex);
            _shell.CmdOut($"Error starting {service.ServiceName}: {ex.Message}");
            _shell.UpdateServicesStates(service.ServiceName, false);
            service.StatusChanged -= OnServiceStatusChangedHandler;
        }
    }

    // --- SERVICE STATUS EVENT HANDLER ---
    private static void OnServiceStatusChangedHandler(object? sender, ServiceStatusEventArgs e)
    {
        if (_shell == null || sender == null) return;

        _shell.UpdateServicesStates(e.ServiceName, e.IsRunning);

        var reason = string.IsNullOrEmpty(e.Message) ? "" : $" Reason: {e.Message}";
        var wasExpectedRunning = RunningServices.ContainsKey(e.ServiceName);

        switch (e.IsRunning)
        {
            case false when wasExpectedRunning:
            {
                Logger.Warn($"Service '{e.ServiceName}' stopped unexpectedly.{reason}");
                _shell.CmdOut($"ALERT: Service '{e.ServiceName}' stopped!{reason}");

                if (RunningServices.TryRemove(e.ServiceName, out _))
                    Logger.Info($"Service '{e.ServiceName}' removed from active list due to unexpected stop.");

                break;
            }
            case true when !wasExpectedRunning:
            {
                Logger.Warn($"Service '{e.ServiceName}' reported running unexpectedly.{reason}");
                if (sender is IService serviceInstance) RunningServices.TryAdd(e.ServiceName, serviceInstance);

                break;
            }
            default:
                Logger.Info(
                    $"Service '{e.ServiceName}' status changed to: {(e.IsRunning ? "Running" : "Stopped")}.{reason}");
                break;
        }
    }

    private static void InitializeStaticLuaBridges()
    {
        try
        {
            Logger.Info("Initializing static bridges for Lua...");

            var allServices = _provider!.GetServices<IService>();
            foreach (var service in allServices)
                switch (service)
                {
                    case TwitchService ttvServ:
                        TwitchTools.Service = ttvServ;
                        break;
                    case AudioService audioServ:
                        Audio.audio = audioServ;
                        break;
                }
        }
        catch (Exception ex)
        {
            Logger.Error("FATAL: Failed to initialize static Lua bridges:", ex);
            throw new InvalidOperationException("Failed to initialize static Lua bridges. See logs for details.", ex);
        }
    }

    private static void UpdateStaticLuaBridges(IService s)
    {
        _shell!.CmdOut($"Updating static bridges for {s.ServiceName}...");
        Logger.Info($"Updating static bridges for {s.ServiceName}...");

        switch (s)
        {
            case TwitchService ttvServ:
                TwitchTools.Service = ttvServ;
                break;
            case AudioService audioServ:
                Audio.audio = audioServ;
                break;
        }

        Logger.Info("Static bridges updated successfully.");
    }
}