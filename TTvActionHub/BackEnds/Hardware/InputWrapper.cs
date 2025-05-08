using System.Runtime.InteropServices;
using static TTvActionHub.BackEnds.Hardware.NativeInputs;

namespace TTvActionHub.BackEnds.Hardware
{
    internal static class InputWrapper
    {
        
        public static bool IsExtendedKey(KeyCode keyCode)
        {
            return keyCode is KeyCode.Alt or KeyCode.LAlt or KeyCode.RAlt or KeyCode.Control or KeyCode.RControl or KeyCode.LControl or KeyCode.Delete or KeyCode.Home or KeyCode.End or KeyCode.Right or KeyCode.Up or KeyCode.Left or KeyCode.Down or KeyCode.NumLock or KeyCode.PrintScreen or KeyCode.Divide;
        }

        public static Input ConstructKeyDown(KeyCode key)
        {
            var down = new Input
            {
                Type = (uint)InputType.Keyboard,
                Data =
                {
                    Keyboard =
                        new KeybdInput
                        {
                            wVk = (ushort) key,
                            wScan = (ushort)(NativeMethods.MapVirtualKey((uint)key, 0) & 0xFFU),
                            dwFlags = (uint) (KeyboardFlag.ScanCode | (IsExtendedKey(key) ? KeyboardFlag.ExtendedKey : 0)),
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                }
            };
            return down;
        }

        public static Input ConstructKeyUp(KeyCode key)
        {
            var up = new Input
            { 
                Type = (uint)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KeybdInput
                    {
                        wVk = (ushort) key,
                        wScan = (ushort)(NativeMethods.MapVirtualKey((uint)key, 0) & 0xFFU),
                        dwFlags = (uint) (KeyboardFlag.ScanCode | KeyboardFlag.KeyUp | (IsExtendedKey(key) ? KeyboardFlag.ExtendedKey : 0)),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            return up;
        }

        public static Input ConstructCharDown(char character)
        {
            ushort scanCode = character;
            var down = new Input
            {
                Type = (ushort)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KeybdInput
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = (uint)KeyboardFlag.Unicode,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            if((scanCode & 0xFF00) == 0xE000)
            {
                down.Data.Keyboard.dwFlags |= (uint)KeyboardFlag.ExtendedKey;
            }
            return down;
        }

        public static Input ConstructCharUp(char character)
        {
            ushort scanCode = character;
            var up = new Input
            {
                Type = (ushort)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KeybdInput
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = (uint)(KeyboardFlag.KeyUp | KeyboardFlag.Unicode),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            if ((scanCode & 0xFF00) == 0xE000)
            {
                up.Data.Keyboard.dwFlags |= (uint)KeyboardFlag.ExtendedKey;
            }
            return up;
        }

        public static Input ConstructAbsoluteMouseMove(int x, int y)
        {
            var move = new Input 
            { 
                Type = (ushort)InputType.Mouse,
                Data =
                {
                    Mouse = new MouseInput
                    {
                        dwFlags = (uint)(MouseFlag.Move | MouseFlag.Absolute),
                        dx = x, dy = y
                    }
                }
            };
            return move;
        }

        public static Input ConstructRelativeMouseMove(int dx, int dy)
        {
            var move = new Input
            {
                Type = (ushort)InputType.Mouse,
                Data =
                {
                    Mouse = new MouseInput
                    {
                        dwFlags = (uint)(MouseFlag.Move),
                        dx = dx, dy = dy
                    }
                }
            };
            return move;
        }

        public static Input ConstructMouseButtonDown(MouseButton button)
        {
            var down = new Input { Type = (ushort)InputType.Mouse };
            down.Data.Mouse.dwFlags = (uint)ToMouseFlag(button, true);
            return down;
        }

        public static Input ConstructMouseButtonUp(MouseButton button)
        {
            var up = new Input { Type = (ushort)InputType.Mouse };
            up.Data.Mouse.dwFlags = (uint)ToMouseFlag(button, false);
            return up;
        }

        public static Input ConstructXMouseButtonUp(int xButtonId)
        {
            var button = new Input { Type = (uint)InputType.Mouse };
            button.Data.Mouse.dwFlags = (uint)MouseFlag.XUp;
            button.Data.Mouse.mouseData = (uint)xButtonId;
            return button;
        }

        public static Input ConstructXMouseButtonDown(int xButtonId)
        {
            var button = new Input { Type = (uint)InputType.Mouse };
            button.Data.Mouse.dwFlags = (uint)MouseFlag.XDown;
            button.Data.Mouse.mouseData = (uint)xButtonId;
            return button;
        }

        public static Input ConstructVWheelScroll(int distance)
        {
            var scroll = new Input { Type = (uint)InputType.Mouse };
            scroll.Data.Mouse.dwFlags = (uint)MouseFlag.VerticalWheel;
            scroll.Data.Mouse.mouseData = (uint)distance;
            return scroll;
        }

        public static Input ConstructHWheelScroll(int distance)
        {
            var scroll = new Input { Type = (uint)InputType.Mouse };
            scroll.Data.Mouse.dwFlags = (uint)MouseFlag.HorizontalWheel;
            scroll.Data.Mouse.mouseData = (uint)distance;
            return scroll;
        }

        public static void DispatchInput(Input[] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs, nameof(inputs));
            if(inputs.Length == 0) throw new ArgumentException("The input array was empty", nameof(inputs));
            var result = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            if(result != inputs.Length)
            {
                throw new Exception("Some simulated input commands were not sent successfully.");
            }
        }

        public static void DispatchInput(IEnumerable<Input> inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs, nameof(inputs));
            var inData = inputs as Input[] ?? inputs.ToArray();
            if (inData.Length == 0) throw new ArgumentException("The input array was empty", nameof(inputs));
            var result = NativeMethods.SendInput((uint)inData.Length, [..inData], Marshal.SizeOf(typeof(Input)));
            if (result != inData.Length)
            {
                throw new Exception("Some simulated input commands were not sent successfully.");
            }
        }

        private static MouseFlag ToMouseFlag(MouseButton button, bool down) => button switch
        {
            MouseButton.LeftButton => down switch
            {
                true => MouseFlag.LeftDown,
                _ => MouseFlag.LeftUp,
            },
            MouseButton.RightButton => down switch
            {
                true => MouseFlag.RightDown,
                _ => MouseFlag.RightUp,
            },
            MouseButton.MiddleButton => down switch
            {
                true => MouseFlag.MiddleDown,
                _ => MouseFlag.MiddleUp,
            },
            _ => MouseFlag.LeftUp,
        };


    }
}
