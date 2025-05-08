using Terminal.Gui;
using NStack;

namespace TTvActionHub.ShellItems
{
    public partial class ScrollableContentView : View
    {
        
        private List<ustring> _lines = [];
        private int _topLine;
        //private Shell _shellRef;

        private Terminal.Gui.Attribute _colorError;
        private Terminal.Gui.Attribute _colorWarning;
        private Terminal.Gui.Attribute _colorInfo;
        private Terminal.Gui.Attribute _colorDefault;
        private Terminal.Gui.Attribute _colorAlert;
        private Terminal.Gui.Attribute _colorInput;

        public ScrollableContentView()
        {
            //_shellRef = shell;
            CanFocus = true;
            InitializeColors();
            KeyPress += HandleKeyPress;
        }

        private void InitializeColors()
        {
            try
            {
                var bg = Colors.Base.Normal.Background;
                if (ColorScheme != null)
                {
                    bg = ColorScheme.Normal.Background;
                }
                _colorError = Application.Driver.MakeAttribute(Color.Red, bg);
                _colorWarning = Application.Driver.MakeAttribute(Color.BrightYellow, bg);
                _colorInfo = Application.Driver.MakeAttribute(Color.Gray, bg);
                _colorDefault = ColorScheme?.Normal ?? Application.Driver.MakeAttribute(Color.White, bg);
                _colorAlert = Application.Driver.MakeAttribute(Color.BrightMagenta, bg);
                _colorInput = Application.Driver.MakeAttribute(Color.Cyan, bg);
            }
            catch
            {
                _colorError = _colorWarning = _colorInfo = _colorDefault = _colorAlert = _colorInput = Colors.Base.Normal;
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
        public void ClearLines() { _lines.Clear(); _topLine = 0; SetNeedsDisplay(); }
        public bool IsScrolledToBottom() { return _topLine >= Math.Max(0, _lines.Count - Bounds.Height); }
        public void ScrollToBottom() { _topLine = Math.Max(0, _lines.Count - Bounds.Height); SetNeedsDisplay(); }
        public void ScrollUp(int lines = 1) { _topLine = Math.Max(0, _topLine - lines); SetNeedsDisplay(); }
        public void ScrollDown(int lines = 1) { _topLine = Math.Min(Math.Max(0, _lines.Count - Bounds.Height), _topLine + lines); SetNeedsDisplay(); }
        public void PageUp() { ScrollUp(Bounds.Height); }
        public void PageDown() { ScrollDown(Bounds.Height); }
        

        public override void OnDrawContent(Rect contentArea)
        {
        
            var bg = ColorScheme?.Normal.Background ?? Color.Black;
            _colorError = Application.Driver.MakeAttribute(Color.Red, bg);
            _colorWarning = Application.Driver.MakeAttribute(Color.BrightYellow, bg);
            _colorInfo = Application.Driver.MakeAttribute(Color.Gray, bg);
            _colorDefault = ColorScheme?.Normal ?? Application.Driver.MakeAttribute(Color.White, bg);
            _colorAlert = Application.Driver.MakeAttribute(Color.BrightMagenta, bg);
            _colorInput = Application.Driver.MakeAttribute(Color.Cyan, bg);


            var driver = Application.Driver;
            driver.SetAttribute(_colorDefault); 
            Clear(); 

            if (_lines.Count == 0) return;

            var viewHeight = Bounds.Height;

            for (var viewRow = 0; viewRow < viewHeight; viewRow++)
            {
                var currentLineIndex = _topLine + viewRow;
                if (currentLineIndex >= _lines.Count) break;

                Move(0, viewRow); 
                var lineUstr = _lines[currentLineIndex];
                var lineStr = lineUstr.ToString() ?? string.Empty; 

                
                var currentAttribute = ParseLineForAttribute(lineStr);
                driver.SetAttribute(currentAttribute);

                
                driver.AddStr(lineUstr);

                
                var drawnLength = lineUstr.ConsoleWidth;
                if (drawnLength >= Bounds.Width) continue;
                driver.SetAttribute(_colorDefault); 
                for (var i = drawnLength; i < Bounds.Width; i++)
                {
                    AddRune(i, viewRow, ' '); 
                }
            }

            
            driver.SetAttribute(_colorDefault);
            var lastDrawnLine = Math.Min(viewHeight, _lines.Count - _topLine);
            for (var i = lastDrawnLine; i < viewHeight; i++)
            {
                Move(0, i);
                for (var j = 0; j < Bounds.Width; j++) { AddRune(j, i, ' '); }
            }
        }

        private Terminal.Gui.Attribute ParseLineForAttribute(string line)
        {
            if (string.IsNullOrEmpty(line)) return _colorDefault;

            if (line.Contains("ERR", StringComparison.OrdinalIgnoreCase) || line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase)) return _colorError;
            if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase) || line.Contains("WARN:", StringComparison.OrdinalIgnoreCase)) return _colorWarning;
            if (line.Contains("INFO", StringComparison.OrdinalIgnoreCase)) return _colorInfo;
            return line.TrimStart().StartsWith('>') ? _colorInput : _colorDefault;
        }

        private void HandleKeyPress(KeyEventEventArgs args)
        {
            
            if (_lines.Count <= Bounds.Height) { args.Handled = false; return; }
            switch (args.KeyEvent.Key)
            {
                case Key.CursorUp: ScrollUp(); args.Handled = true; break;
                case Key.CursorDown: ScrollDown(); args.Handled = true; break;
                case Key.PageUp: PageUp(); args.Handled = true; break;
                case Key.PageDown: PageDown(); args.Handled = true; break;
                case Key.Home: _topLine = 0; SetNeedsDisplay(); args.Handled = true; break;
                case Key.End: ScrollToBottom(); args.Handled = true; break;
                default: args.Handled = false; break;
            }
        }

        public override bool OnMouseEvent(MouseEvent mouseEvent)
        {
            if (mouseEvent.View == this)
            {
                if (_lines.Count <= Bounds.Height)
                {
                    return false;
                }
                if (mouseEvent.Flags.HasFlag(MouseFlags.WheeledDown))
                {
                    ScrollDown();
                    return true; // Событие обработано
                }
                if (mouseEvent.Flags.HasFlag(MouseFlags.WheeledUp))
                {
                    ScrollUp();
                    return true; // Событие обработано
                }
            }

            return base.OnMouseEvent(mouseEvent);
        }
    }
}