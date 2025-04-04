using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public class TimerActionsService(IConfig config) : IService
    {
        private readonly IConfig _config = config;

        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;

        public void Run()
        {
            if(_config.TActions.Count == 0)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to run. Skipping...");
                return;
            }
            foreach (var (_, e) in _config.TActions)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Running [{e.Name}] action");
                e.Run();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All actions are running");
            OnStatusChanged(true);
        }

        public void Stop()
        {
            if (_config.TActions.Count == 0)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to stop. Skipping...");
                return;
            }
            foreach (var (_, e) in _config.TActions)
            {
                if (!e.IsRunning) continue;
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Stopping [{e.Name}] action");
                e.Stop();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All action stopped");
            OnStatusChanged(false);
        }

        public string ServiceName { get => "TimerActions"; }

        public bool IsRunning => _config.TActions.Any((e) => e.Value.IsRunning);

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
