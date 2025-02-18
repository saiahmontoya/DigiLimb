using System.Runtime.InteropServices;

#if WINDOWS
namespace DigiLimb
{
    class KeyboardSimulator
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        public static void PressKey(string key)
        {
            byte keyCode = (byte)Enum.Parse(typeof(ConsoleKey), key);
            keybd_event(keyCode, 0, 0, 0); // Key Down
            keybd_event(keyCode, 0, 2, 0); // Key Up
        }
    }

    class MouseSimulator
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;

        public static void MoveCursor(int dx, int dy)
        {
            mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, IntPtr.Zero);
        }
    }
}
#endif
