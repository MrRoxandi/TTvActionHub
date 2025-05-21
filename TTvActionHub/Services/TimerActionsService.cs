using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Managers;

namespace TTvActionHub.Services;

public sealed class TimerActionsService : IService, IUpdatableConfiguration
{
    public ConcurrentDictionary<string, TimerAction>? Actions { get; private set; }
    public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
    private readonly LuaConfigManager _configManager;
    private readonly IConfig _config;

    public TimerActionsService(IConfig config, LuaConfigManager manager)
    {
        _config = config;
        _configManager = manager;
        var tActions = _configManager.LoadTActions() ?? throw new Exception($"Bad configuration for {ServiceName}");
        Actions = tActions;
    }

    public void Run()
    {
        if (Actions == null)
        {
            OnStatusChanged(false);
            return;
        }

        if (Actions.IsEmpty)
        {
            Logger.Log(LogType.Info, ServiceName, "Nothing to run. Skipping...");
            OnStatusChanged(true);
            return;
        }

        foreach (var (_, e) in Actions)
        {
            Logger.Log(LogType.Info, ServiceName, $"Running [{e.Name}] action");
            e.Run();
        }

        Logger.Log(LogType.Info, ServiceName, "All actions are running");
        OnStatusChanged(true);
    }

    public void Stop()
    {
        if (Actions == null)
        {
            OnStatusChanged(false);
            return;
        }

        if (Actions.IsEmpty)
        {
            Logger.Log(LogType.Info, ServiceName, "Nothing to stop. Skipping...");
            OnStatusChanged(false);
            return;
        }

        foreach (var (_, e) in Actions)
        {
            if (!e.IsRunning) continue;
            Logger.Log(LogType.Info, ServiceName, $"Stopping [{e.Name}] action");
            e.Stop();
        }

        Logger.Log(LogType.Info, ServiceName, "All action stopped");
        OnStatusChanged(false);
    }

    public bool UpdateConfiguration()
    {
        if (_configManager.LoadTActions() is not { } tActions) return false;
        if (Actions is { IsEmpty: false })
            foreach (var (_, e) in Actions)
            {
                if (!e.IsRunning) continue;
                Logger.Log(LogType.Info, ServiceName, $"Stopping [{e.Name}] action");
                e.Stop();
            }

        Actions = tActions;
        foreach (var (_, e) in Actions)
        {
            Logger.Log(LogType.Info, ServiceName, $"Running [{e.Name}] action");
            e.Run();
        }

        return true;
    }

    public string ServiceName => "TimerActions";

    public bool IsRunning => Actions != null;

    private void OnStatusChanged(bool isRunning, string? message = null)
    {
        try
        {
            StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "Error invoking StatusChanged event handler.", ex);
        }
    }
}