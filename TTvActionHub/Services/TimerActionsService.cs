using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Managers;

namespace TTvActionHub.Services
{
    public class TimerActionsService : IService, IUpdatableConfiguration
    {
        public ConcurrentDictionary<string, TimerAction>? TActions { get; set; }
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        private readonly LuaConfigManager _configManager;
        private readonly IConfig _config;

        public TimerActionsService(IConfig config, LuaConfigManager manager)
        {
            _config = config;
            _configManager = manager;
            var tActions = _configManager.LoadTActions() ?? throw new Exception($"Bad configuration for {ServiceName}");
            TActions = tActions;
        }

        public void Run()
        {
            if (TActions == null)
            {
                OnStatusChanged(false);
                return;
            }
            if (TActions.IsEmpty)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to run. Skipping...");
                OnStatusChanged(false);
                return;
            }
            foreach (var (_, e) in TActions)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Running [{e.Name}] action");
                e.Run();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All actions are running");
            OnStatusChanged(true);
        }

        public void Stop()
        {
            if (TActions == null)
            {
                OnStatusChanged(false);
                return;
            }
            if (TActions.IsEmpty)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to stop. Skipping...");
                OnStatusChanged(false);
                return;
            }
            foreach (var (_, e) in TActions)
            {
                if (!e.IsRunning) continue;
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Stopping [{e.Name}] action");
                e.Stop();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All action stopped");
            OnStatusChanged(false);
        }

        public bool UpdateConfiguration()
        {
            if (_configManager.LoadTActions() is not ConcurrentDictionary<string, TimerAction> tActions)
            {
                return false;
            }
            TActions = tActions;
            return true;
        }

        public string ServiceName { get => "TimerActions"; }

        public bool IsRunning => TActions != null && !TActions.IsEmpty && TActions.Any(kvp => kvp.Value.IsRunning);

        protected virtual void OnStatusChanged(bool isRunning, string? message = null)
        {
            try
            {
                StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error invoking StatusChanged event handler.", ex);
            }

        }
    }
}
