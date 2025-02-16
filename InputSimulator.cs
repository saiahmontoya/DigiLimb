using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DigiLimbDesktop
{
    public static class InputSimulator
    {
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        public static void SimulateKeyPress(char key)
        {
            INPUT input = new INPUT
            {
                Type = 1, // Keyboard input
                U = new InputUnion
                {
                    Ki = new KEYBDINPUT
                    {
                        Wvk = 0,
                        WScan = (ushort)key,
                        DwFlags = 0,
                        DwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
        }

        // Define the INPUT struct with Explicit Layout
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint Type;
            public InputUnion U; // Ensure this field is correctly used to access Ki
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT Ki; // Keyboard input struct within the union
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort Wvk; // Virtual-key code for the key
            public ushort WScan; // Hardware scan code for the key
            public uint DwFlags; // Flags specifying various function options
            public uint Time; // Time stamp for the event
            public IntPtr DwExtraInfo; // Extra information associated with the keystroke
        }
    }
}