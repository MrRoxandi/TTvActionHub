using Lua;
using System.Diagnostics;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services;

namespace TTvActionHub.Items
{
    public struct TwitchEventArgs
    {
        public string Sender;
        public string[]? Args;
        public LuaState State;
        public TwitchTools.PermissionLevel Permission;
    }

    public class TwitchEvent
    {
        // --- Main things in event ---
        public TwitchTools.TwitchEventKind Kind { get; private set; }
        private LuaFunction Function { get; }
        public string Name { get; }

        // --- Other stuff ---
        private readonly TwitchTools.PermissionLevel _permissionLevel;
        private readonly Stopwatch? _coolDownTimer;
        private readonly long? _timeOut;
        public readonly long Cost;

        // --- Executing checks ---
        public bool Executable => IsExecutable();

        public static string ItemName => nameof(TwitchEvent);

        public TwitchEvent(TwitchTools.TwitchEventKind kind, LuaFunction action, string name, TwitchTools.PermissionLevel? permissionLevel = null, long? timeOut = null, long cost = 0)
        {
            Kind = kind;
            Function = action;
            Name = name;
            if (permissionLevel is not { } perm)
                perm = TwitchTools.PermissionLevel.Viewer;
            _permissionLevel = perm;
            if (timeOut is not { } time) return;
            _coolDownTimer = new();
            _timeOut = time;
            Cost = cost;
        }

        public void Execute(TwitchEventArgs args)
        {
            if (args.Permission < _permissionLevel)
            {
                Logger.Log(LogType.Info, ItemName, $"Unable to execute event [{Name}]. {args.Sender} has no permission to do that");
                return;
            }
            try
            {
                if (!IsExecutable())
                {
                    Logger.Log(LogType.Info, ItemName, $"Unable to execute event [{Name}]. Event still on cooldown");
                    return;
                }
                var action = args.Args != null ? 
                    Function.InvokeAsync(args.State, [args.Sender, ..args.Args]) :
                    Function.InvokeAsync(args.State, [args.Sender]);
                _ = action.AsTask().GetAwaiter().GetResult();
                _coolDownTimer?.Restart();
                Logger.Log(LogType.Info, ItemName, $"Event [{Name}] was executed successfully");
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to execute event [{Name}] due to error:", e);
            }
        }

        private bool IsExecutable()
        {
            if (_timeOut == null) return true;
            return !_coolDownTimer!.IsRunning || _coolDownTimer!.ElapsedMilliseconds > _timeOut;
        }
        
    }
}
