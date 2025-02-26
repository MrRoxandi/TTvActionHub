using System;
using System.Diagnostics;
using NLua;
using TwitchController.Logs;

namespace TwitchController.Items
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
                Logger.Error($"Unable to run reward: {ex.Message}");
                return;
            }
        }
    }
}
