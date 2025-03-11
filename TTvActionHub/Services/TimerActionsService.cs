using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public class TimerActionsService(IConfig config) : IService
    {
        private readonly IConfig _config = config;

        public void Run()
        {
            if(_config.TActions.Count == 0)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to run. Skipping...");
                return;
            }
            foreach (var e in _config.TActions)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Running [{e.Name}] event");
                e.Run();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All events are running");
        }

        public void Stop()
        {
            if (_config.TActions.Count == 0)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Nothing to stop. Skipping...");
                return;
            }
            foreach (var e in _config.TActions)
            {
                if (!e.IsRunning) continue;
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Stopping [{e.Name}] event");
                e.Stop();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All events stopped");
        }

        public string ServiceName { get => "TimerEventsService"; }
    }
}
