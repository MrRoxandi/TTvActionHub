using NLua;
using TTvActionHub.Logs;

namespace TTvActionHub.Items
{
    public class TwitchReward : IAction 
    {
        public required LuaFunction Function { get; set; }

        public void Execute(string sender, string[]? args)
        {
            try
            {
                Function.Call(sender, args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to run reward due to error:", ex);
                return;
            }
        }
    }
}
