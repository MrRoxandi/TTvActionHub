using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TTvActionHub.LuaTools.Hardware
{
    public static class Mouse
    {
        // --- Public methods ---

        public enum MouseButton : byte
        {
            Left = 0,
            Middle = 1,
            Right = 2,
        }

        public struct Point(int x, int y)
        {
            public int x = x;
            public int y = y;
        }

        public static void Press(MouseButton button)
        {
            var code = GetKeyCode(button, true);
            mouse_event((int)code, 0, 0, 0, 0);
        }

        public static void Release(MouseButton button)
        {
            var code = GetKeyCode(button, false);
            mouse_event((int)code, 0, 0, 0, 0);
        }

        public static void Hold(MouseButton button, int timeDelay = 1000)
        {
            Press(button);
            Thread.Sleep(timeDelay);
            Release(button);
        }

        public static void Click(MouseButton button)
        {
            Press(button);
            Thread.Sleep(100);
            Release(button);
        }

        public static Point GetPosition()
        {
            var gotPoint = GetCursorPos(out Point currentMousePoint);
            if (!gotPoint) { currentMousePoint = new(0, 0); }
            return currentMousePoint;
        }

        public static void SetPosition(Point p)
        {
            SetCursorPos(p.x, p.y);
        }

        public static void SetPostion(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void Move(int dx, int dy)
        {
            mouse_event((int)KeyCodes.Move, dx, dy, 0, 0);
        }

        public static void Move(Point point)
        {
            mouse_event((int)KeyCodes.Move, point.x, point.y, 0, 0);
        }

        // --- Private (backend) methods ---

        private enum KeyCodes : byte
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Point lpMousePoint);

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        private static KeyCodes GetKeyCode(MouseButton button, bool pressed)
            => button switch
            {
                MouseButton.Left => pressed switch
                {
                    true => KeyCodes.LeftDown,
                    _ => KeyCodes.LeftUp,
                },
                MouseButton.Middle => pressed switch
                {
                    true => KeyCodes.MiddleDown,
                    _ => KeyCodes.MiddleUp,
                },
                MouseButton.Right => pressed switch
                {
                    true => KeyCodes.RightDown,
                    _ => KeyCodes.RightUp,
                },
                _ => throw new IndexOutOfRangeException($"Undefined Key: {nameof(button)}")
            };

    }
}
