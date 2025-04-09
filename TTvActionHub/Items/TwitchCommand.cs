using System.Diagnostics;
using NLua;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Stuff;
namespace TTvActionHub.Items
{
    public class TwitchCommand : IAction
    {
        private readonly Stopwatch _coolDownTimer = new();
        public required LuaFunction Function { get; set; }
        public required Users.USERLEVEL Perm;
        public long? TimeOut;
        
        public void Execute(string sender, Users.USERLEVEL level, string[]? args)
        {
            if(level < Perm)
            {
                Logger.Error($"Unable to run command. {sender} has no permission to do that");
                return;
            }
            try
            {
                if (_coolDownTimer.IsRunning && _coolDownTimer.ElapsedMilliseconds < TimeOut)
                    return;
                Function.Call(sender, args);
                _coolDownTimer.Restart();
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to run command for user: {sender} due to error:", ex);
                
                return;
            }
        }
    }
}
