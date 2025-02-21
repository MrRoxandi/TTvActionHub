using System;
using System.Diagnostics;
using NLua;

namespace TwitchController.Items
{
    public class Command
    {
        private readonly Stopwatch _coolDownTimer = new();
        public required LuaFunction Function;
        public string? Description;
        public long? TimeOut;

        public void Execute(string sender, string[]? args)
        {
            try
            {
                if (_coolDownTimer.IsRunning && _coolDownTimer.ElapsedMilliseconds < TimeOut)
                    return;
                _coolDownTimer.Restart();
                Function.Call(sender, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Unable to run command.\n{ex.Message}");
                return;
            }
        }
    }
}
