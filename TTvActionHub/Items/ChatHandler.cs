using NLua;
using TTvActionHub.Logs;

namespace TTvActionHub.Items
{
    public class ChatHandler
    {
        public required LuaFunction Action;
        public required Func<string, string[]?, bool> Condition;
        private bool _isRunning = false;

        public bool IsRunning { get => _isRunning; }

        public void Run()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Execute(string sender, string[]? args)
        {
            if (!_isRunning)
            {
                return;
            }
            if (Condition(sender, args))
            {
                try
                {
                    Action.Call(sender, args);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Unable to run Chat Handler for user: {sender} due to error:", ex);
                    return;
                }
            }
        }
    }
}
