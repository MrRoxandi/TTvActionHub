using Terminal.Gui;
using NStack;
using System.Text;

namespace TTvActionHub.ShellItems
{
    public class HeaderStatusView : View
    {
        private IEnumerable<KeyValuePair<string, bool>> _serviceStatesData = Enumerable.Empty<KeyValuePair<string, bool>>();
        
        private Terminal.Gui.Attribute _attrSuccess;
        private Terminal.Gui.Attribute _attrError;
        private Terminal.Gui.Attribute _attrNormal;

        public HeaderStatusView()
        {
            CanFocus = false; 
            InitializeColors();
        }

        private void InitializeColors()
        {
            try
            {
                var bg = ColorScheme?.Normal.Background ?? Color.Blue; 
                _attrSuccess = Application.Driver.MakeAttribute(Color.Green, bg);
                _attrError = Application.Driver.MakeAttribute(Color.Red, bg);
                _attrNormal = ColorScheme?.Normal ?? Application.Driver.MakeAttribute(Color.White, bg);
            }
            catch
            {
                _attrSuccess = _attrError = _attrNormal = Colors.Base.Normal;
            }
        }

        public void SetData(IEnumerable<KeyValuePair<string, bool>> states)
        {
            _serviceStatesData = states ?? Enumerable.Empty<KeyValuePair<string, bool>>();
            SetNeedsDisplay();
        }

        public override void OnDrawContent(Rect contentArea)
        {
            var bg = ColorScheme?.Normal.Background ?? Color.Blue;
            _attrSuccess = Application.Driver.MakeAttribute(Color.Green, bg);
            _attrError = Application.Driver.MakeAttribute(Color.Red, bg);
            _attrNormal = ColorScheme?.Normal ?? Application.Driver.MakeAttribute(Color.White, bg);


            var driver = Application.Driver;
            driver.SetAttribute(_attrNormal); 
            Clear(); 

            int line = 0;
            foreach (var kvp in _serviceStatesData.OrderBy(s => s.Key))
            {
                if (line >= Bounds.Height) break;

                Move(0, line); 

                
                driver.SetAttribute(_attrNormal);
                driver.AddStr($"{kvp.Key}: ");

                string statusText = kvp.Value ? "Running" : "Stopped";
                Terminal.Gui.Attribute statusAttribute = kvp.Value ? _attrSuccess : _attrError;
                driver.SetAttribute(statusAttribute);
                driver.AddStr(statusText);

                driver.SetAttribute(_attrNormal);
                
                int currentCursorCol = kvp.Key.Length + 2 + statusText.Length;
                for (int i = currentCursorCol; i < Bounds.Width; i++)
                {
                    AddRune(i, line, ' '); 
                }

                line++;
            }

            
            driver.SetAttribute(_attrNormal);
            for (int i = line; i < Bounds.Height; i++)
            {
                Move(0, i);
                for (int j = 0; j < Bounds.Width; j++) { AddRune(j, i, ' '); }
            }
        }
    }
}