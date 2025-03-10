using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public class TActionsService(IConfig config) : IService
    {
        private readonly IConfig _config = config;

        public void Run()
        {
            foreach (var e in _config.TActions)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Running [{e.Name}] event");
                e.Run();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All events are running");
        }

        public void Stop()
        {
            foreach (var e in _config.TActions)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Stopping [{e.Name}] event");
                e.Stop();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All events stopped");
        }

        public string ServiceName { get => "TEventsService"; }
    }
}
