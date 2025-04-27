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
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_SHIFT = 0x10;

        private static bool isShiftPressed = false;
        private static bool isCapsToggled = false;

        public static void ProcessKeyPress(string keyData)
        {
            Debug.WriteLine($"⌨️ Processing Key: {keyData}");

            // Handle toggles
            if (keyData.ToUpper() == "SHIFT")
            {
                isShiftPressed = !isShiftPressed;
                Debug.WriteLine($"🔀 Shift toggled: {isShiftPressed}");
                return;
            }

            if (keyData.ToUpper() == "CAPS")
            {
                keybd_event(VK_CAPITAL, 0, 0, UIntPtr.Zero); // CapsLock Down
                keybd_event(VK_CAPITAL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // CapsLock Up
                isCapsToggled = !isCapsToggled;
                Debug.WriteLine($"🔁 Caps Lock toggled: {isCapsToggled}");
                return;
            }

            // Parse modifiers from keyData
            bool useCtrl = keyData.Contains("Ctrl+");
            bool useAlt = keyData.Contains("Alt+");
            bool useShiftExplicit = keyData.Contains("Shift+");

            // Strip modifiers
            string key = keyData.Replace("Ctrl+", "")
                                .Replace("Alt+", "")
                                .Replace("Shift+", "");

            byte virtualKey = GetVirtualKeyCode(key);
            if (virtualKey == 0)
            {
                Debug.WriteLine($"❌ Unrecognized key: {key}");
                return;
            }

            bool isLetter = key.Length == 1 && char.IsLetter(key[0]);
            bool applyShift =
                isShiftPressed ||
                useShiftExplicit ||
                (isCapsToggled && isLetter && !useShiftExplicit);

            // Press modifiers
            if (useCtrl) keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
            if (useAlt) keybd_event(0x12, 0, 0, UIntPtr.Zero);  // Alt down
            if (applyShift) keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero); // Shift down

            // Main key
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Release modifiers
            if (applyShift) keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (useAlt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (useCtrl) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            Debug.WriteLine($"✅ Simulated Key Press: {keyData}");

            if (isShiftPressed)
                isShiftPressed = false;
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
                "1" => 0x31,
                "2" => 0x32,
                "3" => 0x33,
                "4" => 0x34,
                "5" => 0x35,
                "6" => 0x36,
                "7" => 0x37,
                "8" => 0x38,
                "9" => 0x39,
                "0" => 0x30,
                "ENTER" => 0x0D,
                "BACKSPACE" => 0x08,
                "TAB" => 0x09,
                "SPACE" => 0x20,
                "-" => 0xBD,
                "=" => 0xBB,
                "[" => 0xDB,
                "]" => 0xDD,
                "\\" => 0xDC,
                ";" => 0xBA,
                "'" => 0xDE,
                "," => 0xBC,
                "." => 0xBE,
                "/" => 0xBF,
                "CAPS" => VK_CAPITAL,
                "SHIFT" => VK_SHIFT,
                _ => 0
            };
        }
    }
}
