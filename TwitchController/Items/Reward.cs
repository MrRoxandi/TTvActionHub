using System;
using System.Diagnostics;
using NLua;

namespace TwitchController.Items
{
    public class Reward
    {
        public required LuaFunction Function;
        public string? Description;

        public object[]? Execute(string sender, params object[]? args)
        {

            try
            {
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
