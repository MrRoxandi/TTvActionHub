using Microsoft.VisualBasic;
using System;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace TwitchController.LuaTools.Hardware
{
    public static class Keyboard
    {
        public enum Key : ushort
        {
            F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73,
            F5 = 0x74, F6 = 0x75, F7 = 0x76, F8 = 0x77,
            F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,

            A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45,
            F = 0x46, G = 0x47, H = 0x48, I = 0x49, J = 0x4A,
            K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E, O = 0x4F,
            P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54,
            U = 0x55, V = 0x56, W = 0x57, X = 0x58, Y = 0x59,
            Z = 0x5A,

            NUM_0 = 0x30, NUM_1 = 0x31, NUM_2 = 0x32, NUM_3 = 0x33,
            NUM_4 = 0x34, NUM_5 = 0x35, NUM_6 = 0x36, NUM_7 = 0x37,
            NUM_8 = 0x38, NUM_9 = 0x39,

            ENTER = 0x0D, ESCAPE = 0x1B, BACKSPACE = 0x08,
            TAB = 0x09, SPACE = 0x20, SHIFT = 0x10,
            CONTROL = 0x11, ALT = 0x12, CAPS_LOCK = 0x14,
            LEFT = 0x25, UP = 0x26, RIGHT = 0x27, DOWN = 0x28
        }

        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

        public static void PressKey(Key k)
        {
            keybd_event((byte)k, 0, KEYEVENTF_EXTENDEDKEY, nuint.Zero);
        }

        public static void ReleaseKey(Key k)
        {
            keybd_event((byte)k, 0, KEYEVENTF_KEYUP, nuint.Zero);
        }

        public static async Task TypeKeyAsync(Key key)
        {
            PressKey(key);
            await Task.Delay(50);
            ReleaseKey(key);
        }

        public static async Task HoldKeyAsync(Key k, int duration = 50)
        {
            PressKey(k);
            await Task.Delay(duration);
            ReleaseKey(k);
        }
    }
}
