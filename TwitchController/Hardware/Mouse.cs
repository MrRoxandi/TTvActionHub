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

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

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

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
        }

        public static async Task MoveAsync(int x, int y)
        {
            await Task.Run(() => SetCursorPos(x, y));
        }

        public static async Task HoldAsync(MouseButton button, int timeDelay = 1000)
        {
            await PressAsync(button);
            await Task.Delay(timeDelay);
            await ReleaseAsync(button);
        }

        public static async Task ClickAsync(MouseButton button)
        {
            await PressAsync(button);
            await ReleaseAsync(button);
        }

        public static async Task PressAsync(MouseButton button)
        {
            await Task.Run(() => SendMouseEvent(GetDownFlag(button)));
        }

        public static async Task ReleaseAsync(MouseButton button)
        {
            await Task.Run(() => SendMouseEvent(GetUpFlag(button)));
        }

        public static async Task ScrollAsync(int delta)
        {
            await Task.Run(() => SendMouseEvent(MOUSEEVENTF_WHEEL, delta));
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
            INPUT[] inputs = new INPUT[]
            {
                new INPUT
                {
                    type = 0, // INPUT_MOUSE
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)data,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}