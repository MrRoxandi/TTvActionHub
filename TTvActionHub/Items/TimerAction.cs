using NLua;
using TTvActionHub.Logs;

namespace TTvActionHub.Items
{
    public class TimerAction : IAction
    {
        public required LuaFunction Function { get; set; }
        public bool IsRunning => _timer != null;
        private System.Timers.Timer? _timer;
        public required string Name;
        public required long TimeOut;

        public void Run()
        {
            
            _timer = new(TimeOut);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer is not null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    Function.Call();
                    Logger.Info($"Timer event [{Name}] was executed at [{e.SignalTime}]");
                }
                catch (Exception ex)
                {
                    Logger.Error($"While executing timer event [{Name}] occured an error", ex);
                    Logger.Info($"Stopping timer event [{Name}]");
                    this.Stop();
                }
            });
        }
    }
}
