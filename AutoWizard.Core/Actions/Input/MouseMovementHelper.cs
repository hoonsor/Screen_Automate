using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoWizard.Core.Actions.Input
{
    public static class MouseMovementHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg; public ushort wParamL; public ushort wParamH;
        }

        private const uint INPUT_MOUSE = 0;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private static Random _random = new Random();

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static void MoveAbsolute(int x, int y)
        {
            int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
            int absoluteX = (x * 65536) / screenWidth;
            int absoluteY = (y * 65536) / screenHeight;

            var input = new INPUT { type = INPUT_MOUSE };
            input.union.mi.dx = absoluteX;
            input.union.mi.dy = absoluteY;
            input.union.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
            
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void MoveMouseSmoothly(int startX, int startY, int endX, int endY, int durationMs = 500)
        {
            // 如果距離很短，直接移動
            double distance = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
            if (distance < 10)
            {
                MoveAbsolute(endX, endY);
                return;
            }

            // 產生貝茲曲線控制點
            // 控制點在起點和終點之間，並加入隨機偏移
            int controlX = (startX + endX) / 2 + _random.Next(-100, 100);
            int controlY = (startY + endY) / 2 + _random.Next(-100, 100);

            // 步數取決於持續時間 (每 10ms 一步)
            int steps = Math.Max(10, durationMs / 10);
            
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                
                // 二次貝茲曲線公式
                // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                double u = 1 - t;
                double tt = t * t;
                double uu = u * u;

                int x = (int)(uu * startX + 2 * u * t * controlX + tt * endX);
                int y = (int)(uu * startY + 2 * u * t * controlY + tt * endY);

                MoveAbsolute(x, y);
                Thread.Sleep(10); // 控制速度
            }
            
            // 確保最後位置準確
            MoveAbsolute(endX, endY);
        }

        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT p);
            return new Point(p.X, p.Y);
        }
    }
}
