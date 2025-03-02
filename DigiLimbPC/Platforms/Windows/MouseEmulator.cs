using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DigiLimbDesktop.Platforms.Windows
{
    public static class MouseEmulator
    {
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const int INPUT_MOUSE = 0;
        const int MOUSEEVENTF_MOVE = 0x0001;
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const int MOUSEEVENTF_RIGHTUP = 0x0010;

        private static bool isMoving = false;

        public static void StartMouseMovement()
        {
            isMoving = true;
        }

        public static void StopMouseMovement()
        {
            isMoving = false;
        }

        public static void SimulateMouseMove(double dx, double dy)
        {
            if (!isMoving)
                return;
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dx = (int)dx, dy = (int)dy, dwFlags = MOUSEEVENTF_MOVE } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        const int maxSpeed = 10;
        public static int currentX = 0;
        public static int currentY = 0;

        public static void MoveMouseSmoothly(double x, double y)
        {
            double deltaX = x - currentX;
            double deltaY = y - currentY;

            /*
            int moveX = 0;
            int moveY = 0;

            int steps = 30;
            double stepX = deltaX / steps;
            double stepY = deltaY / steps;

            for (int i=0;i<steps;i++)
            {
                moveX = (int)(currentX + stepX * i);
                moveY = (int)(currentY + stepY * i);

                SimulateMouseMove(moveX, moveY);
                
                Thread.Sleep(5);
            }
            currentX = moveX;
            currentY = moveY;
            */
            double distance = Math.Sqrt(deltaX * deltaY + deltaY * deltaY);
            double steps = Math.Ceiling(distance / maxSpeed);

            double stepsX = deltaX / steps;
            double stepsY = deltaY / steps;

            for (int i = 0; i < steps; i++)
            {
                int moveX = (int)(currentX + stepsX);
                int moveY = (int)(currentY + stepsY);

                SimulateMouseMove(moveX, moveY);

                currentX = moveX;
                currentY = moveY;
                Thread.Sleep(5);
            }
        }

        public static void SimulateLeftClick()
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } };
            inputs[1] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SimulateLeftPress()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        public static void SimulateLeftRelease()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SimulateRightClick()
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTDOWN } };
            inputs[1] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTUP } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SimulateRightPress()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTDOWN } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        public static void SimulateRightRelease()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTUP } };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

    }
}
