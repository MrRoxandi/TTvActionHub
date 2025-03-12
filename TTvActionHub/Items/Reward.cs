using NLua;
using TTvActionHub.Logs;

namespace TTvActionHub.Items
{
    public class Reward
    {
        public required LuaFunction Function;

        public void Execute(string sender, string[]? args)
        {

            try
            {
                Function.Call(sender, args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to run reward due to error:", ex.Message);
                return;
            }
        }
    }
}
