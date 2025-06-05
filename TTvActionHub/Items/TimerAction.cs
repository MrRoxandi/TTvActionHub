using Lua;
using TTvActionHub.Logs;
using Timer = System.Timers.Timer;

namespace TTvActionHub.Items
{
    public class TimerAction : IAction
    {
        public required LuaFunction Function { get; set; }
        public bool IsRunning => _timer != null;
        public required long TimeOut;
        public required string Name;
        private LuaState State;
        private Timer? _timer;
        public void Run(LuaState state)
        {
            
            _timer = new Timer(TimeOut);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
            State = state;
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
                    _ = Function.InvokeAsync(State, []).AsTask().GetAwaiter().GetResult();
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
