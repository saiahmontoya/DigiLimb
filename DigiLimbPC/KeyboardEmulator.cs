using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WindowsInput;
using WindowsInput.Native;

namespace DigiLimbDesktop
{
    public static class KeyboardEmulator
    {
        private static readonly InputSimulator sim = new InputSimulator();

        public static void ProcessKeyPress(string keyData)
        {
            Debug.WriteLine($"⌨️ Processing Key: {keyData}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SimulateKeyPressWindows(keyData);
            }
            else
            {
                Debug.WriteLine($"❌ Unsupported OS for key input: {RuntimeInformation.OSDescription}");
            }
        }

        private static void SimulateKeyPressWindows(string keyData)
        {
            try
            {
                // Split modifier keys
                bool ctrl = keyData.Contains("Ctrl+");
                bool alt = keyData.Contains("Alt+");
                bool shift = keyData.Contains("Shift+");
                string key = keyData.Replace("Ctrl+", "").Replace("Alt+", "").Replace("Shift+", "");

                if (!Enum.TryParse($"VK_{key.ToUpper()}", out VirtualKeyCode vKey))
                {
                    Debug.WriteLine($"❌ Unrecognized key: {key}");
                    return;
                }

                // Press modifier keys
                if (ctrl) sim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                if (alt) sim.Keyboard.KeyDown(VirtualKeyCode.MENU);
                if (shift) sim.Keyboard.KeyDown(VirtualKeyCode.SHIFT);

                // Press key
                sim.Keyboard.KeyPress(vKey);

                // Release modifier keys
                if (ctrl) sim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                if (alt) sim.Keyboard.KeyUp(VirtualKeyCode.MENU);
                if (shift) sim.Keyboard.KeyUp(VirtualKeyCode.SHIFT);

                Debug.WriteLine($"✅ Simulated Key: {keyData}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Windows Key Simulation Error: {ex.Message}");
            }
        }
    }
}
