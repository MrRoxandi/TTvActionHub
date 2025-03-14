﻿using TTvActionHub.Logs;

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
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Running [{e.Name}] action");
                e.Run();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All actions are running");
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
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Stopping [{e.Name}] action");
                e.Stop();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "All action stopped");
        }

        public string ServiceName { get => "TimerActionsService"; }
    }
}
