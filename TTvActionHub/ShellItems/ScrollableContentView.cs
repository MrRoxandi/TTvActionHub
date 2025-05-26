using NStack;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace TTvActionHub.ShellItems;

public partial class ScrollableContentView : View
{
    private List<ustring> _lines = [];
    private int _topLine;

    private Attribute _colorError;
    private Attribute _colorWarning;
    private Attribute _colorInfo;
    private Attribute _colorInput;

    public ScrollableContentView()
    {
        base.CanFocus = false;
        KeyPress += HandleKeyPress;
    }

    public override ColorScheme ColorScheme
    {
        get => base.ColorScheme;
        set
        {
            base.ColorScheme = value;
            var bg = value.Normal.Background;
            _colorError = Application.Driver.MakeAttribute(Color.Red, bg);
            _colorInfo = Application.Driver.MakeAttribute(Color.Gray, bg);
            _colorInput = Application.Driver.MakeAttribute(Color.Cyan, bg);
            _colorWarning = Application.Driver.MakeAttribute(Color.BrightYellow, bg);
            SetNeedsDisplay();
        }
    }

    public void SetLines(IEnumerable<ustring> newLines)
    {
        var wasAtBottom = IsScrolledToBottom();
        _lines = newLines.ToList();
        if (wasAtBottom) ScrollToBottom();
        else _topLine = Math.Min(_topLine, Math.Max(0, _lines.Count - Bounds.Height));
        SetNeedsDisplay();
    }

    public void AddLine(ustring line)
    {
        var wasAtBottom = IsScrolledToBottom();
        _lines.Add(line);
        if (wasAtBottom) ScrollToBottom();
        SetNeedsDisplay();
    }

    public void ClearLines()
    {
        _lines.Clear();
        _topLine = 0;
        SetNeedsDisplay();
    }

    public bool IsScrolledToBottom()
    {
        return _topLine >= Math.Max(0, _lines.Count - Bounds.Height);
    }

    public void ScrollToBottom()
    {
        _topLine = Math.Max(0, _lines.Count - Bounds.Height);
        SetNeedsDisplay();
    }

    public void ScrollUp(int lines = 1)
    {
        _topLine = Math.Max(0, _topLine - lines);
        SetNeedsDisplay();
    }

    public void ScrollDown(int lines = 1)
    {
        _topLine = Math.Min(Math.Max(0, _lines.Count - Bounds.Height), _topLine + lines);
        SetNeedsDisplay();
    }

    public void PageUp()
    {
        ScrollUp(Bounds.Height);
    }

    public void PageDown()
    {
        ScrollDown(Bounds.Height);
    }


    public override void OnDrawContent(Rect contentArea)
    {
        var driver = Application.Driver;
        var defaultAttribute = GetNormalColor();

        driver.SetAttribute(defaultAttribute);
        Clear();
        if (_lines.Count == 0) return;

        var viewHeight = Bounds.Height;

        for (var viewRow = 0; viewRow < viewHeight; viewRow++)
        {
            var currentLineIndex = _topLine + viewRow;
            if (currentLineIndex >= _lines.Count) break;

            Move(0, viewRow);
            var lineUStr = _lines[currentLineIndex];
            var lineStr = lineUStr.ToString() ?? string.Empty;


            var currentAttribute = ParseLineForAttribute(lineStr);
            driver.SetAttribute(currentAttribute);


            driver.AddStr(lineUStr);


            var drawnLength = lineUStr.ConsoleWidth;
            if (drawnLength >= Bounds.Width) continue;
            driver.SetAttribute(defaultAttribute);
            for (var i = drawnLength; i < Bounds.Width; i++) AddRune(i, viewRow, ' ');
        }


        driver.SetAttribute(defaultAttribute);
        var lastDrawnLine = Math.Min(viewHeight, _lines.Count - _topLine);
        for (var i = lastDrawnLine; i < viewHeight; i++)
        {
            Move(0, i);
            for (var j = 0; j < Bounds.Width; j++) AddRune(j, i, ' ');
        }
    }

    private Attribute ParseLineForAttribute(string line)
    {
        if (string.IsNullOrEmpty(line)) return GetNormalColor();

        if (line.Contains("ERR", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Error:", StringComparison.OrdinalIgnoreCase)) return _colorError;
        if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("WARN:", StringComparison.OrdinalIgnoreCase)) return _colorWarning;
        if (line.Contains("Info", StringComparison.OrdinalIgnoreCase)) return _colorInfo;
        return line.TrimStart().StartsWith('>') ? _colorInput : GetNormalColor();
    }

    private void HandleKeyPress(KeyEventEventArgs args)
    {
        if (_lines.Count <= Bounds.Height)
        {
            args.Handled = false;
            return;
        }

        switch (args.KeyEvent.Key)
        {
            case Key.CursorUp:
                ScrollUp();
                args.Handled = true;
                break;
            case Key.CursorDown:
                ScrollDown();
                args.Handled = true;
                break;
            case Key.PageUp:
                PageUp();
                args.Handled = true;
                break;
            case Key.PageDown:
                PageDown();
                args.Handled = true;
                break;
            case Key.Home:
                _topLine = 0;
                SetNeedsDisplay();
                args.Handled = true;
                break;
            case Key.End:
                ScrollToBottom();
                args.Handled = true;
                break;
            default: args.Handled = false; break;
        }
    }

    public override bool OnMouseEvent(MouseEvent mouseEvent)
    {
        if (mouseEvent.View != this) return base.OnMouseEvent(mouseEvent);
        if (_lines.Count <= Bounds.Height) return false;
        if (mouseEvent.Flags.HasFlag(MouseFlags.WheeledDown))
        {
            ScrollDown();
            return true;
        }

        if (!mouseEvent.Flags.HasFlag(MouseFlags.WheeledUp)) return base.OnMouseEvent(mouseEvent);
        ScrollUp();
        return true;
    }
}