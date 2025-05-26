using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace TTvActionHub.ShellItems;

public partial class HeaderStatusView : View
{
    private IEnumerable<KeyValuePair<string, bool>> _serviceStatesData = [];

    private Attribute _attrSuccess;
    private Attribute _attrError;

    public override ColorScheme ColorScheme
    {
        get => base.ColorScheme;
        set
        {
            base.ColorScheme = value;
            var bg = value.Normal.Background;
            _attrSuccess = Application.Driver.MakeAttribute(Color.Green, bg);
            _attrError = Application.Driver.MakeAttribute(Color.Red, bg);
            SetNeedsDisplay();
        }
    }

    public HeaderStatusView()
    {
        base.CanFocus = false;
    }

    public void SetData(IEnumerable<KeyValuePair<string, bool>> states)
    {
        _serviceStatesData = states;
        SetNeedsDisplay();
    }

    public override void OnDrawContent(Rect contentArea)
    {
        var driver = Application.Driver;
        var defaultAttribute = GetNormalColor();

        driver.SetAttribute(defaultAttribute);
        Clear();

        var line = 0;
        foreach (var kvp in _serviceStatesData.OrderBy(s => s.Key))
        {
            if (line >= Bounds.Height) break;

            Move(0, line);


            driver.SetAttribute(defaultAttribute);
            driver.AddStr($"{kvp.Key}: ");

            var statusText = kvp.Value ? "Running" : "Stopped";
            var statusAttribute = kvp.Value ? _attrSuccess : _attrError;
            driver.SetAttribute(statusAttribute);
            driver.AddStr(statusText);

            driver.SetAttribute(defaultAttribute);

            var currentCursorCol = kvp.Key.Length + 2 + statusText.Length;
            for (var i = currentCursorCol; i < Bounds.Width; i++) AddRune(i, line, ' ');

            line++;
        }

        driver.SetAttribute(defaultAttribute);
    }
}