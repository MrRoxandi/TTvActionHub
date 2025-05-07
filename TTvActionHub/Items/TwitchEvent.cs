using System;
using NLua;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Stuff;

namespace TTvActionHub.Items
{
    public struct TwitchEventArgs
    {
        public string Sender;
        public string[]? Args;
        public TwitchTools.PermissionLevel Permission;
    }

    public class TwitchEvent
    {
        // --- Main things in event ---
        public TwitchTools.TwitchEventKind Kind { get; private set; }
        public LuaFunction Function { get; private set; }
        public string Name { get; private set; }

        // --- Other stuff ---
        public TwitchTools.PermissionLevel PermissionLevel;
        public Stopwatch? _coolDownTimer;
        public long? TimeOut;

        // --- Executing checks ---
        public bool Executable => IsExecutable();

        private string ItemName => nameof(TwitchEvent);

        public TwitchEvent(TwitchTools.TwitchEventKind kind, LuaFunction action, string name, TwitchTools.PermissionLevel? permissionLevel = null, long? timeOut = null)
        {
            Kind = kind;
            Function = action;
            Name = name;
            if (permissionLevel is not TwitchTools.PermissionLevel perm)
                perm = TwitchTools.PermissionLevel.VIEWIER;
            PermissionLevel = perm;
            if (timeOut is long time)
            {
                _coolDownTimer = new();
                TimeOut = time;
            }
        }

        public void Execute(TwitchEventArgs args)
        {
            if (args.Permission < PermissionLevel)
            {
                Logger.Log(LOGTYPE.INFO, ItemName, $"Unable to execute event [{Name}]. {args.Sender} has no permission to do that");
                return;
            }
            try
            {
                if (!IsExecutable())
                {
                    Logger.Log(LOGTYPE.INFO, ItemName, $"Unable to execute event [{Name}]. Event still on cooldown");
                    return;
                }
                Function.Call(args.Sender, args.Args);
                _coolDownTimer?.Restart();
                Logger.Log(LOGTYPE.INFO, ItemName, $"Event [{Name}] was executed successfully");
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to execute event [{Name}] due to error:", e);
                return;
            }
        }

        private bool IsExecutable()
        {
            if (TimeOut == null) return true;
            else return !_coolDownTimer!.IsRunning || _coolDownTimer!.ElapsedMilliseconds > TimeOut;
        }
        
    }
}
