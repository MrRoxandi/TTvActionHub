using System;
using System.Diagnostics;
using NLua;
using TwitchController.Logs;

namespace TwitchController.Items
{
    internal class Command
    {
        private readonly Stopwatch _coolDownTimer = new();
        public required LuaFunction Function;
        public long? TimeOut;
        
        public void Execute(string sender, string[]? args)
        {
            try
            {
                if (_coolDownTimer.IsRunning && _coolDownTimer.ElapsedMilliseconds < TimeOut)
                    return;
                Function.Call(sender, args);
                _coolDownTimer.Restart();
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to run command: {ex.Message}");
                
                return;
            }
        }
    }
}
