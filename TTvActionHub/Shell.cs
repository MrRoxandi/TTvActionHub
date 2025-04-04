using System.Collections.Concurrent;
using TTvActionHub.Logs;
using TTvActionHub.Services;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using NStack;
using System.Text;
using TTvActionHub.ShellItems;
using TTvActionHub.Items;

namespace TTvActionHub
{
   public class Shell(IConfig config,
       Action<string>? restartServiceCallBack = null,
       Action? restartServicesCallBack = null,
       IServiceProvider? serviceProvider = null) : IDisposable
   {
        // --- Main config and dependencies ---
        private readonly IConfig _config = config;
        private readonly Action<string>? _restartServiceCallback = restartServiceCallBack;
        private readonly Action? _restartAllServicesCallback = restartServicesCallBack;
        private readonly IServiceProvider? _serviceProvider = serviceProvider;

        // --- UI states ---
        private readonly ConcurrentDictionary<string, bool> _serviceStates = new(StringComparer.OrdinalIgnoreCase); // Service -> Status (Running = true)
        private readonly ConcurrentQueue<string> _commandsOutPutQueue = [];
        private bool _showLogs = false;

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
        private const int _UiUpdateIntervalMs = 150;
        private readonly int _maxCmdHistory = 50;
        private IMainLoopDriver? _mainLoopDriver;
        private List<ustring> _cmdOutputHistory = [];
        private object? _timeoutToken;

        // --- Command line interactions ---
        private List<string> _enteredCommandHistory = [];
        private int _historyIndex = -1;
        private ustring _currentTypedCommand = string.Empty;

        // --- Colors ---

        private ColorScheme? _headerColorScheme;
        private ColorScheme? _bodyColorScheme;
        private ColorScheme? _inputColorScheme;
        private ColorScheme? _statusColorScheme;
        private Terminal.Gui.Attribute _colorError;
        private Terminal.Gui.Attribute _colorWarning;
        private Terminal.Gui.Attribute _colorInfo;
        private Terminal.Gui.Attribute _colorDefault;
        private Terminal.Gui.Attribute _colorSuccess;
                
        public void InitializeUI()
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
            
            _headerTextView = new HeaderStatusView(this)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            _headerFrame.Add(_headerTextView);

            // --- Body --- (Logs / Cmds)

            _bodyTextView = new ScrollableContentView(this)
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
                new StatusItem(Key.F1, "~F1~ Help", () => ShowHelpDialog()),
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
            if (key == Key.Enter)
            {
                args.Handled = true;
                var inputText = _commandInput?.Text.ToString()?[2..] ?? string.Empty;
                _commandInput!.Text = ustring.Make("> ");
                _commandInput!.CursorPosition = _commandInput.Text.Length;

                if (!string.IsNullOrWhiteSpace(inputText))
                {
                    CmdOut($"> {inputText}");

                    if (_enteredCommandHistory.Count == 0 || _enteredCommandHistory[^1] != inputText)
                    {
                        _enteredCommandHistory.Add(inputText);
                        if (_enteredCommandHistory.Count > _maxCmdHistory)
                        {
                            _enteredCommandHistory.RemoveAt(0);
                        }
                    }
                    _historyIndex = -1; 
                    _currentTypedCommand = string.Empty;

                    bool keepRunning = ExecInnerCommand(inputText);
                    if (!keepRunning) { RequestStop(); }
                }
            }
            // --- Commands history: backwards ---
            else if (key == Key.CursorUp) 
            {
                args.Handled = true;
                if (_enteredCommandHistory.Count > 0)
                {
                    if (_historyIndex == -1)
                    {
                        _currentTypedCommand = _commandInput?.Text ?? string.Empty; 
                        _historyIndex = _enteredCommandHistory.Count - 1; 
                    }
                    else if (_historyIndex > 0)
                    {
                        _historyIndex--; 
                    }
                    if (_historyIndex >= 0)
                    {
                        _commandInput!.Text = ustring.Make($"> {_enteredCommandHistory[_historyIndex]}");
                        _commandInput.CursorPosition = _commandInput.Text.Length;
                    }
                }
            }
            // --- Commands history: forward ---
            else if (key == Key.CursorDown)
            {
                args.Handled = true;
                if (_historyIndex != -1) // only if we are in histor 
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
            }
            else if (key == Key.Esc)
            {
                if (_historyIndex != -1)
                {
                    args.Handled = true;
                    _commandInput!.Text = _currentTypedCommand;
                    _commandInput.CursorPosition = _commandInput.Text.Length;
                    _historyIndex = -1;
                }
            }
            else if (_historyIndex != -1 && !(args.KeyEvent.IsShift || args.KeyEvent.IsCtrl || args.KeyEvent.IsAlt))
            {
                _historyIndex = -1;
                
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
                TimeSpan.FromMilliseconds(_UiUpdateIntervalMs),
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

            int serviceCount = statesCopy.Count;
            
            // Some magic to expand header...
            if (_headerFrame != null && _win != null)
            {
                int requiredHeight = Math.Max(3, serviceCount + 2);
                int availableHeight = _win.Bounds.Height - 2;
                requiredHeight = Math.Min(requiredHeight, availableHeight);
                var currentFrameHeight = _headerFrame.Frame.Height;
                if (currentFrameHeight != requiredHeight)
                {
                    _headerFrame.Height = requiredHeight;
                    if (_bodyTextView != null) { _bodyTextView.Y = Pos.Bottom(_headerFrame); }
                    _win.LayoutSubviews();
                }
            }
        }

        private void UpdateBody()
        {
            if (_bodyTextView == null) return;

            bool needsUpdate = false;

            if (!_showLogs)
            {
                bool added = false;
                while (_commandsOutPutQueue.TryDequeue(out string? message))
                {
                    if (message != null)
                    {
                        _cmdOutputHistory.Add(ustring.Make(message));
                        added = true;
                    }
                }
                if (_cmdOutputHistory.Count > _maxCmdHistory)
                {
                    _cmdOutputHistory.RemoveRange(0, _cmdOutputHistory.Count - _maxCmdHistory);
                    needsUpdate = true; 
                }
                if (added) needsUpdate = true; 
            }
            if (_showLogs)
            {
                var logs = Logger.LastLogs() ?? [];
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
            string command = input?.Trim().ToLowerInvariant() ?? "";
            string[] parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string commandName = parts.Length > 0 ? parts[0] : ""; // Main command
            string argument = parts.Length > 1 ? parts[1] : ""; // Args string

            switch (commandName)
            {
                case "": return true;
                case "exit": return false;
                case "clear": CmdClear(); CmdOut("Command output cleared."); return true;
                case "logs": ToggleLogView(true); CmdOut("Switched to logs view."); return true;
                case "cmd": ToggleLogView(false); CmdOut("Switched to command output view."); return true;
                case "restart": HandleRestartCommand(argument); return true; // TODO: Fix bugs
                case "help": ShowHelpDialog(); return true;
                default: CmdOut($"Unknown command: '{input}'. Type 'help' for available commands."); return true;
            }
        }

        private void HandleRestartCommand(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                CmdOut("Usage: restart <service_name>|all");
                return;
            }

            string target = argument.Trim();

            if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (_restartAllServicesCallback != null)
                {
                    CmdOut($"Initiating restart for all running services...");
                    try 
                    { 
                        _restartAllServicesCallback(); 
                    } 
                    catch (Exception ex) 
                    { 
                        HandleCallbackError("restart all", ex); 
                    }
                    // Updating headers
                }
                else { CmdOut("ERROR: 'Restart All' functionality is not configured."); }
            }
            else
            {
                var serviceName = GetServiceNameCaseSensitive(target);
                if (serviceName == null)
                {
                    CmdOut($"ERROR: Service '{target}' not found or not registered.");
                    return;
                }

                if (_restartServiceCallback != null)
                {
                    CmdOut($"Initiating restart for service: {serviceName}...");
                    try 
                    { 
                        _restartServiceCallback(serviceName); 
                    }
                    catch (Exception ex) 
                    { 
                        HandleCallbackError($"restart {serviceName}", ex); 
                    }
                }
                else { CmdOut($"ERROR: 'Restart Service' functionality is not configured."); }
            }
            
        }

        private void ShowHelpDialog()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Available commands:");
            builder.AppendLine("restart <name> - Restart the specified service");
            builder.AppendLine("help - Shows this help message (or press F1)");
            builder.AppendLine("clear - Clears the command output area");
            builder.AppendLine("logs - Switch view to application logs");
            builder.AppendLine("cmd - Switch view to command output");
            builder.AppendLine("exit - Stops services and exits");
            MessageBox.Query("Help", builder.ToString(), "Ok");
        }

        private void HandleCallbackError(string actionName, Exception ex)
        {
            CmdOut($"ERROR during '{actionName}' execution: {ex.Message}");
            Logger.Error($"Exception during '{actionName}' callback execution from Shell:", ex);
        }

        private string? GetServiceNameCaseSensitive(string name)
        {
            var kvp = _serviceStates.FirstOrDefault(p => p.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!kvp.Equals(default(KeyValuePair<string, bool>))) return kvp.Key;
            if (_serviceProvider != null)
            {
                var service = _serviceProvider.GetServices<IService>().FirstOrDefault(s => s.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase));
                return service?.ServiceName;
            }
            return null;
        }

        public void Dispose()
        {
            if (_timeoutToken != null && _mainLoopDriver != null)
            {
                try { Application.MainLoop.RemoveTimeout(_timeoutToken); } catch { /* Ignore */ }
                _timeoutToken = null;
            }
        }
    }
}
