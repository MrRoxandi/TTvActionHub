using System;
using System.Diagnostics;
using NLua;

namespace TwitchController.Items
{
    public class Reward
    {
        private readonly Stopwatch _coolDownTimer = new();
        public LuaFunction? Function;
        public string? Description;
        public long? TimeOut;

        public object[]? Execute(string sender, object[]? args)
        {
            try
            {
                if (_coolDownTimer.IsRunning && _coolDownTimer.ElapsedMilliseconds < TimeOut)
                    return null;
                _coolDownTimer.Restart();
                if (Function is null) return null;
                return Function.Call(sender, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Unable to run reward.\n{ex.Message}");
                return null;
            }
        }
    }
}
