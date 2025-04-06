using System.Runtime.InteropServices;
using TTvActionHub.BackEnds.Hardware;

namespace TTvActionHub.LuaTools.Hardware
{
    public static class Keyboard
    {
        // --- Public blokc ---

        public enum Key : byte
        {
            /// --- Numbers line ---
            D0 = 0x30, D1 = 0x31, D2 = 0x32,
            D3 = 0x33, D4 = 0x34, D5 = 0x35,
            D6 = 0x36, D7 = 0x37, D8 = 0x38,
            D9 = 0x39,
            // --- Alphabet ---
            A = 0x41, B = 0x42, C = 0x43,
            D = 0x44, E = 0x45, F = 0x46,
            G = 0x47, H = 0x48, I = 0x49,
            J = 0x4A, K = 0x4B, L = 0x4C,
            M = 0x4D, N = 0x4E, O = 0x4F,
            P = 0x50, Q = 0x51, R = 0x52,
            S = 0x53, T = 0x54, U = 0x55,
            V = 0x56, W = 0x57, X = 0x58,
            Y = 0x59, Z = 0x5A,
            // --- Numpad ---
            NumLock = 0x90, NumPad0 = 0x60,
            NumPad1 = 0x61, NumPad2 = 0x62,
            NumPad3 = 0x63, NumPad4 = 0x64,
            NumPad5 = 0x65, NumPad6 = 0x66,
            NumPad7 = 0x67, NumPad8 = 0x68,
            NumPad9 = 0x69, Multiply = 0x6A,
            Separator = 0x6C, Add = 0x6B,
            Subtract = 0x6D, Decimal = 0x6E,
            Divide = 0x6F,
            // --- Function keys ---
            F1 = 0x70, F2 = 0x71, F3 = 0x72,
            F4 = 0x73, F5 = 0x74, F6 = 0x75,
            F7 = 0x76, F8 = 0x77, F9 = 0x78,
            F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
            F13 = 0x7C, F14 = 0x7D, F15 = 0x7E,
            F16 = 0x7F, F17 = 0x80, F18 = 0x81,
            F19 = 0x82, F20 = 0x83, F21 = 0x84,
            F22 = 0x85, F23 = 0x86, F24 = 0x87,

            // --- Media related buttons ---
            VolumeMute = 0xAD, VolumeDown = 0xAE,
            VolumeUp = 0xAF,

            MediaStop = 0xB2,
            MediaNextTrack = 0xB0,
            MediaPlayPause = 0xB3,
            MediaPreviousTrack = 0xB1,

            // --- Shift keys ---
            Shift = 0x10, RShiftKey = 0xA1, LShiftKey = 0xA0,

            // --- Alt keys ---
            Alt = 0x12, LAlt = 0xA4, RAlt = 0xA5,

            // --- Control keys ---
            Control = 0x11, LControlKey = 0xA2, RControlKey = 0xA3,

            // --- Arrow keys ---
            Up = 0x26, Right = 0x27, Left = 0x25, Down = 0x28,

            // --- Extra buttons ---
            Win = 0x5B,         Sleep = 0x5F,
            Backspace = 0x08,   Tab = 0x09,
            LineFeed = 0x0A,    Clear = 0x0C,
            Enter = 0x0D,       Pause = 0x13,
            CapsLock = 0x14,    Escape = 0x1B,
            Space = 0x20,       PageUp = 0x21,
            PageDown = 0x22,    End = 0x23,
            Home = 0x24,        PrintScreen = 0x2C,
            Insert = 0x2D,      Delete = 0x2E,
            Scroll = 0x91,

        }

        public static void PressKey(Key k)
        {
            var input = InputWrapper.ConstructKeyDown((NativeInputs.KeyCode)k);
            InputWrapper.DispatchInput([input]);
        }

        public static void ReleaseKey(Key k)
        {
            var input = InputWrapper.ConstructKeyUp((NativeInputs.KeyCode)k);
            InputWrapper.DispatchInput([input]);
        }

        public static void TypeMessage(string message)
        {
            List<NativeInputs.INPUT> inputs = [];
            foreach(char c in message)
            {
                inputs.Add(InputWrapper.ConstructCharDown(c));
                inputs.Add(InputWrapper.ConstructCharUp(c));
            }
            InputWrapper.DispatchInput(inputs);
        }

        public static void TypeKey(Key key)
        {
            PressKey(key);
            Thread.Sleep(50);
            ReleaseKey(key);
        }

        public static void HoldKey(Key k, int duration = 1000)
        {
            if(duration < 200)
            {
                TypeKey(k);
                return;
            }
            var durStep = duration / 100;
            for(var totalDuration = 0; totalDuration < duration; totalDuration += durStep)
            {
                PressKey(k);
                Thread.Sleep(durStep);
                ReleaseKey(k);
            }
        }

    }
}
