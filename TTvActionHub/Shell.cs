using System.Collections.Concurrent;
using TTvActionHub.ShellItems;
using TTvActionHub.Logs;
using Terminal.Gui;
using System.Text;
using NStack;

namespace TTvActionHub
{
   public partial class Shell(IConfig config,
       Action<string>? startServicesCallBack = null,
       Action<string>? stopServiceCallBack = null,
       Action<string>? reloadServiceCallBack = null,
       Func<string, string[]?>? listServiceCallBack = null) : IDisposable
   {

        // --- Main config and dependencies ---
        private readonly IConfig _config = config;

        // --- UI states ---
        private readonly ConcurrentDictionary<string, bool> _serviceStates = new(StringComparer.OrdinalIgnoreCase); // Service -> Status (Running = true)
        private readonly ConcurrentQueue<string> _commandsOutPutQueue = [];
        private bool _showLogs;

        // --- UI for Terminal.GUI ---

        private Toplevel? _top;
        private Window? _win;
        private FrameView? _headerFrame;
        private FrameView? _commandInputFrame;
        private HeaderStatusView? _headerTextView;
        private ScrollableContentView? _bodyTextView;
        private TextField? _commandInput;
        private StatusBar? _statusBar;
        private StatusItem? _modeStatusItem;

        // --- Control Updates ---
        private const int UiUpdateIntervalMs = 150;
        private const int MaxCmdHistory = 50;
        private IMainLoopDriver? _mainLoopDriver;
        private readonly List<ustring> _cmdOutputHistory = [];
        private object? _timeoutToken;

        // --- Command line interactions ---
        private readonly List<string> _enteredCommandHistory = [];
        private int _historyIndex = -1;
        private ustring _currentTypedCommand = string.Empty;

        // --- Colors ---

        private ColorScheme? _headerColorScheme;
        private ColorScheme? _bodyColorScheme;
        private ColorScheme? _inputColorScheme;
        private ColorScheme? _statusColorScheme;
                
        public void InitializeUi()
        {
            Application.Init();
            _top = Application.Top;
            _mainLoopDriver = Application.MainLoop.Driver;
            _headerColorScheme = new()
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.Black),
            };

            _bodyColorScheme = new()
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
            };
            _inputColorScheme = new()
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
            };
            _statusColorScheme = new()
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.Gray, Color.Black)
            };

            // --- Main window ---

            _win = new Window("TTvActionHub")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = _headerColorScheme
            };
            
            // --- Header ----

            _headerFrame = new FrameView("Services Status")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 3, // Basic... Will be changed at runtime
                ColorScheme = _headerColorScheme
            };
            
            _headerTextView = new HeaderStatusView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            _headerFrame.Add(_headerTextView);

            // --- Body --- (Logs / Cmds)

            _bodyTextView = new ScrollableContentView
            {
                X = 0,
                Y = Pos.Bottom(_headerFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill(3), 
                ColorScheme = _bodyColorScheme
            };

            // --- Commands input ---
            _commandInputFrame = new FrameView("Input")
            {
                X = 0,
                Y = Pos.Bottom(_bodyTextView),
                Width = Dim.Fill(),
                Height = 3,
                ColorScheme = _bodyColorScheme
            };

            _commandInput = new TextField("")
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Height = 1,
                ColorScheme = _inputColorScheme
            };
            _commandInputFrame.Add(_commandInput);

            _commandInput.KeyDown += OnCommandInputKeyDown;
            _commandInput.Text = ustring.Make("> ");
            _commandInput.CursorPosition = _commandInput.Text.Length;
            _modeStatusItem = new StatusItem(Key.Null, "Mode: CMD", null);

            _statusBar = new StatusBar([
                _modeStatusItem,
                new StatusItem(Key.F1, "~F1~ Help", ShowHelpDialog),
                new StatusItem(Key.F2, "~F2~ Logs", () => ToggleLogView(true)),
                new StatusItem(Key.F3, "~F3~ Cmds", () => ToggleLogView(false))
            ])
            {
                ColorScheme = _statusColorScheme,
                Visible = true
            };

            _win.Add(_headerFrame, _bodyTextView, _commandInputFrame);
            _top.Add(_win, _statusBar);
        }

        private void OnCommandInputKeyDown(View.KeyEventEventArgs args)
        {
            var key = args.KeyEvent.Key;
            switch (key)
            {
                case Key.Enter:
                {
                    args.Handled = true;
                    if(_commandInput?.Text.Length < 2)
                    {
                        _commandInput.Text = ustring.Make("> ");
                    } 
                    var inputText = _commandInput?.Text.ToString()?[2..] ?? string.Empty;
                    _commandInput!.Text = ustring.Make("> ");
                    _commandInput!.CursorPosition = _commandInput.Text.Length;

                    if (!string.IsNullOrWhiteSpace(inputText))
                    {
                        CmdOut($"> {inputText}");

                        if (_enteredCommandHistory.Count == 0 || _enteredCommandHistory[^1] != inputText)
                        {
                            _enteredCommandHistory.Add(inputText);
                            if (_enteredCommandHistory.Count > MaxCmdHistory)
                            {
                                _enteredCommandHistory.RemoveAt(0);
                            }
                        }
                        _historyIndex = -1; 
                        _currentTypedCommand = string.Empty;

                        var keepRunning = ExecInnerCommand(inputText);
                        if (!keepRunning) { RequestStop(); }
                    }

                    break;
                }
                // --- Commands history: backwards ---
                case Key.CursorUp:
                {
                    args.Handled = true;
                    if (_enteredCommandHistory.Count > 0)
                    {
                        switch (_historyIndex)
                        {
                            case -1:
                                _currentTypedCommand = _commandInput?.Text ?? string.Empty; 
                                _historyIndex = _enteredCommandHistory.Count - 1;
                                break;
                            case > 0:
                                _historyIndex--;
                                break;
                        }

                        if (_historyIndex >= 0)
                        {
                            _commandInput!.Text = ustring.Make($"> {_enteredCommandHistory[_historyIndex]}");
                            _commandInput.CursorPosition = _commandInput.Text.Length;
                        }
                    }

                    break;
                }
                // --- Commands history: forward ---
                case Key.CursorDown:
                {
                    args.Handled = true;
                    if (_historyIndex != -1) // only if we are in history 
                    {
                        if (_historyIndex < _enteredCommandHistory.Count - 1)
                        {
                            _historyIndex++; 
                            _commandInput!.Text = ustring.Make($"> {_enteredCommandHistory[_historyIndex]}");
                        }
                        else 
                        {
                            _historyIndex = -1;
                            _commandInput!.Text = _currentTypedCommand;
                        }
                        _commandInput.CursorPosition = _commandInput.Text.Length; 
                    }

                    break;
                }
                case Key.Esc:
                {
                    if (_historyIndex != -1)
                    {
                        args.Handled = true;
                        _commandInput!.Text = _currentTypedCommand;
                        _commandInput.CursorPosition = _commandInput.Text.Length;
                        _historyIndex = -1;
                    }

                    break;
                }
                default:
                {
                    if (_historyIndex != -1 && !(args.KeyEvent.IsShift || args.KeyEvent.IsCtrl || args.KeyEvent.IsAlt))
                    {
                        _historyIndex = -1;
                
                    }

                    break;
                }
            }
        }

        private void RequestStop()
        {
            if (_timeoutToken != null && _mainLoopDriver != null)
            {
                Application.MainLoop.RemoveTimeout(_timeoutToken);
                _timeoutToken = null;
            }
            Application.RequestStop();
        }

        public void Run()
        {
            if (_top == null)
            {
                Logger.Error("UI not initialized before calling Run(). Call InitializeUI() first.");
                return;
            }
            _timeoutToken = Application.MainLoop.AddTimeout(
                TimeSpan.FromMilliseconds(UiUpdateIntervalMs),
                UpdateTimerCallback
            );

            Application.Run(_top); // Это блокирующий вызов

        }

        private bool UpdateTimerCallback(MainLoop mainLoop)
        {
            UpdateHeader();
            UpdateBody();
            
            if (Application.Top?.MostFocused != _commandInput && _commandInput?.CanFocus == true)
            {
                _commandInput?.FocusFirst();
            }

            return true;
        }

        private void UpdateHeader()
        {
            if (_headerTextView == null) return;

            var statesCopy = _serviceStates.ToList();
            _headerTextView.SetData(statesCopy);

            var serviceCount = statesCopy.Count;
            
            // Some magic to expand header...
            if (_headerFrame == null || _win == null) return;
            var requiredHeight = Math.Max(3, serviceCount + 2);
            var availableHeight = _win.Bounds.Height - 2;
            requiredHeight = Math.Min(requiredHeight, availableHeight);
            var currentFrameHeight = _headerFrame.Frame.Height;
            if (currentFrameHeight == requiredHeight) return;
            _headerFrame.Height = requiredHeight;
            if (_bodyTextView != null) { _bodyTextView.Y = Pos.Bottom(_headerFrame); }
            _win.LayoutSubviews();
        }

        private void UpdateBody()
        {
            if (_bodyTextView == null) return;

            var needsUpdate = false;

            if (!_showLogs)
            {
                var added = false;
                while (_commandsOutPutQueue.TryDequeue(out var message))
                {
                    _cmdOutputHistory.Add(ustring.Make(message));
                    added = true;
                }
                if (_cmdOutputHistory.Count > MaxCmdHistory)
                {
                    _cmdOutputHistory.RemoveRange(0, _cmdOutputHistory.Count - MaxCmdHistory);
                    needsUpdate = true; 
                }
                if (added) needsUpdate = true; 
            }
            if (_showLogs)
            {
                var logs = Logger.LastLogs();
                _bodyTextView.SetLines(logs.Select(s => (ustring)s));
            }
            else if (needsUpdate)
            { 
                _bodyTextView.SetLines(_cmdOutputHistory);
            }

        }

        public void AddService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return;
            _serviceStates.TryAdd(serviceName, false);
            CmdOut($"Service '{serviceName}' registered.");
        }

        public void UpdateServicesStates(string serviceName, bool newState)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return;
            if (_serviceStates.ContainsKey(serviceName))
            {
                _serviceStates[serviceName] = newState;
            }
        }

        public void CmdOut(string message)
        {
            _commandsOutPutQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void CmdClear()
        {
            while (_commandsOutPutQueue.TryDequeue(out _)) { }
            _cmdOutputHistory.Clear();
            if (!_showLogs && _bodyTextView != null)
            {
                Application.MainLoop.Invoke(() => _bodyTextView.ClearLines());
            }
        }

        private void ToggleLogView(bool showLogs)
        {
            if (_showLogs == showLogs) return;
            _showLogs = showLogs;
            _modeStatusItem!.Title = $"Mode: {(_showLogs ? "LOGS" : "CMD")}";
            _bodyTextView?.ClearLines(); 
            _statusBar?.SetNeedsDisplay();
        }

        // --- Private block ---

        private bool ExecInnerCommand(string? input)
        {
            var command = input?.Trim().ToLowerInvariant() ?? "";
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts.Length > 0 ? parts[0] : ""; // Main command
            var argument = parts.Length > 1 ? parts[1] : ""; // Args string

            switch (commandName)
            {
                case "": return true;
                case "cmd": ToggleLogView(false); CmdOut("Switched to command output view."); return true;
                case "logs": ToggleLogView(true); CmdOut("Switched to logs view."); return true;
                case "clear": CmdClear(); CmdOut("Command output cleared."); return true;
                case "start": HandleStartCommand(argument); return true;
                case "stop": HandleStopCommand(argument); return true;
                case "info": HandleInfoCommand(argument); return true;
                case "reload": HandleReloadCommand(argument); return true;
                case "help": ShowHelpDialog(); return true;
                case "exit": return false;
                default: CmdOut($"Unknown command: '{input}'. Type 'help' for available commands."); return true;
            }
        }

        private void HandleInfoCommand(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                CmdOut($"Usage: info [<service>|{string.Join('|', _serviceStates.Keys)}]");
                return;
            }
            var target = argument.Trim();
            var serviceName = _serviceStates.FirstOrDefault(
                kvp => kvp.Key.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(serviceName))
            {
                CmdOut($"Unable to find: {target}");
                CmdOut($"Usage: info [<field>|{string.Join('|', _serviceStates.Keys)}]");
                return;
            }
            if (listServiceCallBack == null)
            {
                CmdOut("Getting info about service is not configured. Ignoring...");
                return;
            }
            try
            {
                var information = listServiceCallBack(serviceName);
                if (information == null) return;
                if (information.Length == 0)
                {
                    CmdOut($"Service: {serviceName} -> Actions: empty");
                    return;
                }
                CmdOut($"Service: {serviceName} -> Actions: [{string.Join(',', information)}]");
            }
            catch (Exception ex)
            {
                HandleCallbackError($"info {serviceName}", ex);
            }

        }

        private void HandleStopCommand(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                CmdOut($"Usage: stop [<service_name>|{string.Join('|', _serviceStates.Keys)}]");
                return;
            }
            var target = argument.Trim();
            var serviceName = _serviceStates.FirstOrDefault(
                kvp => kvp.Key.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(serviceName))
            {
                CmdOut($"Unable to find service: {serviceName}");
                return;
            }
            if(stopServiceCallBack != null)
            {
                try
                {
                    stopServiceCallBack(serviceName);
                }
                catch (Exception ex)
                {
                    HandleCallbackError($"stop {serviceName}", ex);
                }
            } else
            {
                CmdOut("Stop service functionality is not configured. Ignoring...");
            }
        }

        private void HandleStartCommand(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                CmdOut($"Usage: start [<service_name>|{string.Join('|', _serviceStates.Keys)}]");
                return;
            }
            var target = argument.Trim();
            var serviceName = _serviceStates.FirstOrDefault(
                kvp => kvp.Key.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(serviceName))
            {
                CmdOut($"Unable to find service: {serviceName}");
                return;
            }
            if (startServicesCallBack != null)
            {
                try
                {
                    startServicesCallBack(serviceName);
                }
                catch (Exception ex)
                {
                    HandleCallbackError($"start {serviceName}", ex);
                }
            }
            else
            {
                CmdOut("Start service functionality is not configured. Ignoring...");
            }
        }

        private void HandleReloadCommand(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                CmdOut($"Usage: reload [<service_name>|{string.Join('|', _serviceStates.Keys)}]");
                return;
            }

            var target = argument.Trim();
            var serviceName = _serviceStates.FirstOrDefault(
                kvp => kvp.Key.Equals(target, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(serviceName))
            {
                CmdOut($"Unable to find service: {serviceName}");
                return;
            }

            if (reloadServiceCallBack != null)
            {
                try
                {
                    reloadServiceCallBack(serviceName);
                }
                catch (Exception ex)
                {
                    HandleCallbackError($"reload {serviceName}", ex);
                }
            }
            else
            {
                CmdOut("Reload service functionality is not configured. Ignoring...");
            }
        }

        private static void ShowHelpDialog()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Available commands:");
            builder.AppendLine("reload <name> - Attempt to reload configuration of specified service");
            builder.AppendLine("stop <name> - Attempt to stop the specified service");
            builder.AppendLine("start <name> - Attempt to start specified service");
            builder.AppendLine("info <name> - Show info about service");
            builder.AppendLine("clear - Clears the command output area");
            builder.AppendLine("logs - Switch view to application logs");
            builder.AppendLine("cmd - Switch view to command output");
            builder.AppendLine("help - Shows this help message");
            builder.AppendLine("exit - Stops services and exits");
            MessageBox.Query("Available commands", builder.ToString(), "Ok");
        }

        private void HandleCallbackError(string actionName, Exception ex)
        {
            CmdOut($"Error during '{actionName}' execution: {ex.Message}");
            Logger.Error($"Exception during '{actionName}' callback execution from Shell:", ex);
        }

        public void Dispose()
        {
            if (_timeoutToken == null || _mainLoopDriver == null) return;
            try { Application.MainLoop.RemoveTimeout(_timeoutToken); } catch { /* Ignore */ }
            _timeoutToken = null;
        }
    }
}
