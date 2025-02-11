using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TwitchController.Hardware
{
    public static class Mouse
    {
        public enum MouseButton
        {
            Left,
            Right,
            Middle
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint INPUT_MOUSE = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public static void SetPosition(int x, int y)
        {
            if (!SetCursorPos(x, y))
                throw new InvalidOperationException("Failed to move cursor.");
        }

        public static async Task MoveAsync(int targetX, int targetY, int steps = 50, int delay = 5)
        {
            if (!GetCursorPos(out POINT startPos))
                throw new InvalidOperationException("Failed to get cursor position.");

            int startX = startPos.X;
            int startY = startPos.Y;

            for (int i = 0; i <= steps; i++)
            {
                int x = startX + (targetX - startX) * i / steps;
                int y = startY + (targetY - startY) * i / steps;
                SetCursorPos(x, y);
                await Task.Delay(delay);
            }
        }

        public static async Task HoldAsync(MouseButton button, int timeDelay = 1000)
        {
            Press(button);
            await Task.Delay(timeDelay);
            Release(button);
        }

        public static void Click(MouseButton button)
        {
            Press(button);
            Release(button);
        }

        public static void Press(MouseButton button)
        {
            SendMouseEvent(GetDownFlag(button));
        }

        public static void Release(MouseButton button)
        {
            SendMouseEvent(GetUpFlag(button));
        }

        public static void Scroll(int delta)
        {
            SendMouseEvent(MOUSEEVENTF_WHEEL, delta);
        }

        private static uint GetDownFlag(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
                MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
                _ => throw new ArgumentOutOfRangeException(nameof(button))
            };
        }

        private static uint GetUpFlag(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTUP,
                MouseButton.Right => MOUSEEVENTF_RIGHTUP,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
                _ => throw new ArgumentOutOfRangeException(nameof(button))
            };
        }

        private static void SendMouseEvent(uint flags, int data = 0)
        {
            INPUT input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = (uint)data,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            if (SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new InvalidOperationException("Failed to send mouse input.");
        }
    }
}
