using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DigiLimbDesktop
{
    public static class KeyboardEmulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void ProcessKeyPress(string keyData)
        {
            Debug.WriteLine($"⌨️ Processing Key: {keyData}");

            byte virtualKey = GetVirtualKeyCode(keyData);
            if (virtualKey == 0)
            {
                Debug.WriteLine($"❌ Unrecognized key: {keyData}");
                return;
            }

            keybd_event(virtualKey, 0, 0, UIntPtr.Zero); // Key down
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Key up

            Debug.WriteLine($"✅ Simulated Key Press: {keyData}");
        }

        private static byte GetVirtualKeyCode(string key)
        {
            return key.ToUpper() switch
            {
                "A" => 0x41,
                "B" => 0x42,
                "C" => 0x43,
                "D" => 0x44,
                "E" => 0x45,
                "F" => 0x46,
                "G" => 0x47,
                "H" => 0x48,
                "I" => 0x49,
                "J" => 0x4A,
                "K" => 0x4B,
                "L" => 0x4C,
                "M" => 0x4D,
                "N" => 0x4E,
                "O" => 0x4F,
                "P" => 0x50,
                "Q" => 0x51,
                "R" => 0x52,
                "S" => 0x53,
                "T" => 0x54,
                "U" => 0x55,
                "V" => 0x56,
                "W" => 0x57,
                "X" => 0x58,
                "Y" => 0x59,
                "Z" => 0x5A,
                "ENTER" => 0x0D,
                "BACKSPACE" => 0x08,
                "SPACE" => 0x20,
                "SHIFT" => 0x10,
                "CTRL" => 0x11,
                "ALT" => 0x12,
                "TAB" => 0x09,
                _ => 0
            };
        }
    }
}
