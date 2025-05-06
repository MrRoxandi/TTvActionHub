using System.Runtime.InteropServices;
using static TTvActionHub.BackEnds.Hardware.NativeInputs;

namespace TTvActionHub.BackEnds.Hardware
{
    internal static class InputWrapper
    {
        
        public static bool IsExtendedKey(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Alt || keyCode == KeyCode.LAlt || keyCode == KeyCode.RAlt ||
                keyCode == KeyCode.Control || keyCode == KeyCode.RControl || keyCode == KeyCode.LControl ||
                keyCode == KeyCode.Delete || keyCode == KeyCode.Home || keyCode == KeyCode.End ||
                keyCode == KeyCode.Right || keyCode == KeyCode.Up || keyCode == KeyCode.Left ||
                keyCode == KeyCode.Down || keyCode == KeyCode.NumLock || keyCode == KeyCode.PrintScreen ||
                keyCode == KeyCode.Divide)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static INPUT ConstructKeyDown(KeyCode key)
        {
            var down = new INPUT
            {
                Type = (UInt32)InputType.Keyboard,
                Data =
                {
                    Keyboard =
                        new KEYBDINPUT
                        {
                            wVk = (UInt16) key,
                            wScan = (UInt16)(NativeMethods.MapVirtualKey((UInt32)key, 0) & 0xFFU),
                            dwFlags = (UInt32) (KeyboardFlag.ScanCode | (IsExtendedKey(key) ? KeyboardFlag.ExtendedKey : 0)),
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                }
            };
            return down;
        }

        public static INPUT ConstructKeyUp(KeyCode key)
        {
            var up = new INPUT
            { 
                Type = (UInt32)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = (UInt16) key,
                        wScan = (UInt16)(NativeMethods.MapVirtualKey((UInt32)key, 0) & 0xFFU),
                        dwFlags = (UInt32) (KeyboardFlag.ScanCode | KeyboardFlag.KeyUp | (IsExtendedKey(key) ? KeyboardFlag.ExtendedKey : 0)),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            return up;
        }

        public static INPUT ConstructCharDown(char character)
        {
            UInt16 scanCode = character;
            var down = new INPUT
            {
                Type = (UInt16)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = (UInt32)KeyboardFlag.Unicode,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            if((scanCode & 0xFF00) == 0xE000)
            {
                down.Data.Keyboard.dwFlags |= (UInt32)KeyboardFlag.ExtendedKey;
            }
            return down;
        }

        public static INPUT ConstructCharUp(char character)
        {
            UInt16 scanCode = character;
            var up = new INPUT
            {
                Type = (UInt16)InputType.Keyboard,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = (UInt32)(KeyboardFlag.KeyUp | KeyboardFlag.Unicode),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            if ((scanCode & 0xFF00) == 0xE000)
            {
                up.Data.Keyboard.dwFlags |= (UInt32)KeyboardFlag.ExtendedKey;
            }
            return up;
        }

        public static INPUT ConstructAbsoluteMouseMove(int x, int y)
        {
            var move = new INPUT 
            { 
                Type = (UInt16)InputType.Mouse,
                Data =
                {
                    Mouse = new MOUSEINPUT
                    {
                        dwFlags = (UInt32)(MouseFlag.Move | MouseFlag.Absolute),
                        dx = x, dy = y
                    }
                }
            };
            return move;
        }

        public static INPUT ConstructRelativeMouseMove(int dx, int dy)
        {
            var move = new INPUT
            {
                Type = (UInt16)InputType.Mouse,
                Data =
                {
                    Mouse = new MOUSEINPUT
                    {
                        dwFlags = (UInt32)(MouseFlag.Move),
                        dx = dx, dy = dy
                    }
                }
            };
            return move;
        }

        public static INPUT ConstructMouseButtonDown(MouseButton button)
        {
            var down = new INPUT { Type = (UInt16)InputType.Mouse };
            down.Data.Mouse.dwFlags = (UInt32)ToMouseFlag(button, true);
            return down;
        }

        public static INPUT ConstructMouseButtonUp(MouseButton button)
        {
            var up = new INPUT { Type = (UInt16)InputType.Mouse };
            up.Data.Mouse.dwFlags = (UInt32)ToMouseFlag(button, false);
            return up;
        }

        public static INPUT ConstuctXMouseButtonUp(int xButtonId)
        {
            var button = new INPUT { Type = (UInt32)InputType.Mouse };
            button.Data.Mouse.dwFlags = (UInt32)MouseFlag.XUp;
            button.Data.Mouse.mouseData = (UInt32)xButtonId;
            return button;
        }

        public static INPUT ConstuctXMouseButtonDown(int xButtonId)
        {
            var button = new INPUT { Type = (UInt32)InputType.Mouse };
            button.Data.Mouse.dwFlags = (UInt32)MouseFlag.XDown;
            button.Data.Mouse.mouseData = (UInt32)xButtonId;
            return button;
        }

        public static INPUT ConstructVWheelScroll(int distance)
        {
            var scroll = new INPUT { Type = (UInt32)InputType.Mouse };
            scroll.Data.Mouse.dwFlags = (UInt32)MouseFlag.VerticalWheel;
            scroll.Data.Mouse.mouseData = (UInt32)distance;
            return scroll;
        }

        public static INPUT ConstructHWheelScroll(int distance)
        {
            var scroll = new INPUT { Type = (UInt32)InputType.Mouse };
            scroll.Data.Mouse.dwFlags = (UInt32)MouseFlag.HorizontalWheel;
            scroll.Data.Mouse.mouseData = (UInt32)distance;
            return scroll;
        }

        public static void DispatchInput(INPUT[] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs, nameof(inputs));
            if(inputs.Length == 0) throw new ArgumentException("The input array was empty", nameof(inputs));
            var result = NativeMethods.SendInput((UInt32)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if(result != inputs.Length)
            {
                throw new Exception("Some simulated input commands were not sent successfully.");
            }
        }

        public static void DispatchInput(IEnumerable<INPUT> inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs, nameof(inputs));
            if (!inputs.Any()) throw new ArgumentException("The input array was empty", nameof(inputs));
            var result = NativeMethods.SendInput((UInt32)inputs.Count(), [..inputs], Marshal.SizeOf(typeof(INPUT)));
            if (result != inputs.Count())
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
