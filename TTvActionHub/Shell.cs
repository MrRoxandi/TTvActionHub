using System.Collections.Concurrent;
using NStack;
using Terminal.Gui;
using TTvActionHub.Logs;
using TTvActionHub.Services.Interfaces;
using TTvActionHub.ShellItems;
using TTvActionHub.ShellItems.InnerCommands;
using TTvActionHub.ShellItems.Interfaces;

namespace TTvActionHub;

public partial class Shell(
    Action<string>? startServicesCallBack = null,
    Action<string>? stopServiceCallBack = null,
    Action<string>? reloadServiceCallBack = null,
    Func<string, IService?>? getServiceByNameCallBack = null,
    Func<string, string[]?>? serviceInfoCallBack = null) : IDisposable
{
    // --- Callbacks ---

    public Func<string, string[]?>? ServiceInfoCallBack { get; } = serviceInfoCallBack;
    public Func<string, IService?>? GetServiceByNameCallBack { get; } = getServiceByNameCallBack;
    public Action<string>? StopServiceCallBack { get; } = stopServiceCallBack;
    public Action<string>? StartServiceCallBack { get; } = startServicesCallBack;
    public Action<string>? ReloadServiceCallBack { get; } = reloadServiceCallBack;

    // --- UI states ---
    public ConcurrentDictionary<string, bool> ServiceStates { get; }
        = new(StringComparer.OrdinalIgnoreCase); // Service -> Status (Running = true)


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
    private const int MaxCmdHistory = 350;
    private IMainLoopDriver? _mainLoopDriver;
    private readonly List<ustring> _cmdOutputHistory = [];
    private object? _timeoutToken;

    // --- Command line interactions ---
    private readonly List<string> _enteredCommandHistory = [];
    private ustring _currentTypedCommand = string.Empty;
    private const string CommandPrompt = "> ";
    private int _historyIndex = -1;
    // --- Colors ---

    private ColorScheme? _headerColorScheme;
    private ColorScheme? _bodyColorScheme;
    private ColorScheme? _inputColorScheme;
    private ColorScheme? _statusColorScheme;

    // --- Other stuff ---

    public ConcurrentDictionary<string, IInnerCommand> InnerCommands { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void InitializeUi()
    {
        RegInnerCommands();
        Application.Init();
        _top = Application.Top;
        _mainLoopDriver = Application.MainLoop.Driver;
        _headerColorScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
        };
        _bodyColorScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Gray, Color.Black)
        };
        _inputColorScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
        };
        _statusColorScheme = new ColorScheme
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
            Height = Dim.Fill(),
            ColorScheme = _headerColorScheme
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
        _commandInput.Text = CommandPrompt;
        _commandInput.CursorPosition = _commandInput.Text.Length;
        _modeStatusItem = new StatusItem(Key.Null, "Mode: CMD", null);

        _statusBar = new StatusBar([
            _modeStatusItem,
            new StatusItem(Key.F1, "~F1~ Help", () => new HelpInnerCommand().Execute(this, [])),
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
                if (_commandInput is null) break;
                var text = _commandInput.Text ?? ustring.Empty;
                var userInput = text.ToString() ?? string.Empty;
                var commandInput = userInput.StartsWith(CommandPrompt) ? userInput[CommandPrompt.Length..] : userInput;
                _commandInput!.Text = CommandPrompt;
                _commandInput!.CursorPosition = _commandInput.Text.Length;

                if (!string.IsNullOrWhiteSpace(commandInput))
                {
                    CmdOut($"> {commandInput}");

                    if (_enteredCommandHistory.Count == 0 || _enteredCommandHistory[^1] != commandInput)
                    {
                        _enteredCommandHistory.Add(commandInput);
                        if (_enteredCommandHistory.Count > MaxCmdHistory) _enteredCommandHistory.RemoveAt(0);
                    }

                    _historyIndex = -1;
                    _currentTypedCommand = string.Empty;

                    var commandResult = ExecInnerCommand(commandInput);
                    //if (!keepRunning) Stop();
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
                    _historyIndex = -1;

                break;
            }
        }
    }

    public void Stop()
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
            _commandInput?.FocusFirst();

        return true;
    }

    private void UpdateHeader()
    {
        if (_headerTextView == null) return;

        var statesCopy = ServiceStates.ToList();
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
        if (_bodyTextView != null) _bodyTextView.Y = Pos.Bottom(_headerFrame);
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
                if (string.IsNullOrEmpty(message)) continue;
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
        ServiceStates.TryAdd(serviceName, false);
        CmdOut($"Service '{serviceName}' registered.");
    }

    public void UpdateServicesStates(string serviceName, bool newState)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return;
        if (ServiceStates.ContainsKey(serviceName)) ServiceStates[serviceName] = newState;
    }

    public void CmdOut(string message)
    {
        _commandsOutPutQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void CmdClear()
    {
        while (_commandsOutPutQueue.TryDequeue(out _))
        {
        }

        _cmdOutputHistory.Clear();
        if (!_showLogs && _bodyTextView != null) Application.MainLoop.Invoke(() => _bodyTextView.ClearLines());
    }

    public void ToggleLogView(bool showLogs)
    {
        if (_showLogs == showLogs) return;
        _showLogs = showLogs;
        _modeStatusItem!.Title = $"Mode: {(_showLogs ? "LOGS" : "CMD")}";
        _bodyTextView?.ClearLines();
        _statusBar?.SetNeedsDisplay();
        _bodyTextView?.SetNeedsDisplay();
    }

    // --- Private block ---

    private bool ExecInnerCommand(string? input)
    {
        var userInput = input?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrEmpty(userInput)) return false;
        var parts = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0];
        var commandArgs = parts[1..];
        var found = InnerCommands.TryGetValue(commandName, out var innerCommand);
        if (!found || innerCommand == null)
        {
            CmdOut($"Unknown command '{commandName}'. Check 'help' for more information.");
            return false;
        }

        var result = innerCommand.Execute(this, commandArgs);
        return result;
    }

    private void RegInnerCommands()
    {
        InnerCommands.TryAdd(InfoInnerCommand.CommandName, new InfoInnerCommand());
        InnerCommands.TryAdd(StopInnerCommand.CommandName, new StopInnerCommand());
        InnerCommands.TryAdd(StartInnerCommand.CommandName, new StartInnerCommand());
        InnerCommands.TryAdd(ReloadInnerCommand.CommandName, new ReloadInnerCommand());
        InnerCommands.TryAdd(HelpInnerCommand.CommandName, new HelpInnerCommand());
        InnerCommands.TryAdd(ExitInnerCommand.CommandName, new ExitInnerCommand());
        InnerCommands.TryAdd(LogViewInnerCommand.CommandName, new LogViewInnerCommand());
        InnerCommands.TryAdd(CmdViewInnerCommand.CommandName, new CmdViewInnerCommand());
        InnerCommands.TryAdd(PointsInnerCommand.CommandName, new PointsInnerCommand());
    }

    public string GetProperServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return string.Empty;
        return ServiceStates.Keys.FirstOrDefault(srv => srv.StartsWith(serviceName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }
        
    
    public void HandleCallbackError(string actionName, Exception ex)
    {
        CmdOut($"Error during '{actionName}' execution: {ex.Message}");
        Logger.Error($"Exception during '{actionName}' callback execution from Shell:", ex);
    }

    public void Dispose()
    {
        if (_timeoutToken == null || _mainLoopDriver == null) return;
        try
        {
            Application.MainLoop.RemoveTimeout(_timeoutToken);
        }
        catch
        {
            /* Ignore */
        }

        _timeoutToken = null;
    }
}