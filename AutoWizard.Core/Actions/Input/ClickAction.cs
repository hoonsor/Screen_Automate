using System;
using System.Runtime.InteropServices;
using System.Threading;
using AutoWizard.Core.Models;
using System.Drawing;

namespace AutoWizard.Core.Actions.Input
{
    /// <summary>
    /// 滑鼠按鈕類型
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    /// <summary>
    /// 點擊類型
    /// </summary>
    public enum ClickType
    {
        Single,
        Double,
        Down,
        Up
    }

    /// <summary>
    /// 滑鼠點擊指令
    /// </summary>
    public class ClickAction : BaseAction
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; } = MouseButton.Left;
        public ClickType ClickType { get; set; } = ClickType.Single;

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

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        /// <summary>
        /// X 座標（支援變數表達式，如 "{savedX}"）
        /// </summary>
        /// <summary>
        /// X 座標（支援變數表達式，如 "{savedX}"）
        /// </summary>
        public string XExpression { get; set; } = string.Empty;

        /// <summary>
        /// Y 座標（支援變數表達式，如 "{savedY}"）
        /// </summary>
        public string YExpression { get; set; } = string.Empty;

        /// <summary>
        /// 是否模擬真人滑鼠移動
        /// </summary>
        public bool IsHumanLike { get; set; } = false;
        
        /// <summary>
        /// 模擬移動的持續時間 (毫秒)
        /// </summary>
        public int HumanLikeDurationMs { get; set; } = 500;

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                // 如果有表達式，優先使用表達式解析；否則使用直接座標
                int resolvedX = !string.IsNullOrEmpty(XExpression)
                    ? context.ResolveInt(XExpression, X)
                    : X;
                int resolvedY = !string.IsNullOrEmpty(YExpression)
                    ? context.ResolveInt(YExpression, Y)
                    : Y;

                // 檢查是否綁定特定視窗
                if (context.TargetWindowHandle != IntPtr.Zero)
                {
                    var pt = new POINT { X = resolvedX, Y = resolvedY };
                    if (ClientToScreen(context.TargetWindowHandle, ref pt))
                    {
                        resolvedX = pt.X;
                        resolvedY = pt.Y;
                    }
                    else
                    {
                        context.Log($"Warning: Could not map coordinates to window handle {context.TargetWindowHandle}");
                    }
                }

                // 取得目標解析度以計算絕對座標
                int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
                int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
                
                // 轉換為 0-65535 的絕對座標
                int absoluteX = (resolvedX * 65536) / screenWidth;
                int absoluteY = (resolvedY * 65536) / screenHeight;

                // 移動滑鼠到目標位置
                if (IsHumanLike)
                {
                    var start = MouseMovementHelper.GetCursorPosition();
                    MouseMovementHelper.MoveMouseSmoothly(start.X, start.Y, resolvedX, resolvedY, HumanLikeDurationMs);
                }
                else
                {
                    // 使用 SendInput 進行絕對座標移動，這對拖曳和遊戲相容性較好，不會打斷 Down 狀態
                    var input = new INPUT { type = INPUT_MOUSE };
                    input.union.mi.dx = absoluteX;
                    input.union.mi.dy = absoluteY;
                    input.union.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
                    SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                    
                    Thread.Sleep(50); // 短暫延遲確保移動完成
                }

                // 執行點擊
                switch (ClickType)
                {
                    case ClickType.Single:
                        PerformClick(absoluteX, absoluteY);
                        break;
                    case ClickType.Double:
                        PerformClick(absoluteX, absoluteY);
                        Thread.Sleep(50);
                        PerformClick(absoluteX, absoluteY);
                        break;
                    case ClickType.Down:
                        PerformMouseDown(absoluteX, absoluteY);
                        break;
                    case ClickType.Up:
                        PerformMouseUp(absoluteX, absoluteY);
                        break;
                }

                string contextStr = context.TargetWindowHandle != IntPtr.Zero ? $" relative to '{context.TargetWindowTitle}'" : "";
                context.Log($"Clicked {Button} button at ({resolvedX}, {resolvedY}){contextStr}");

                return new ActionResult
                {
                    Success = true,
                    Message = $"Successfully clicked at ({resolvedX}, {resolvedY}){contextStr}"
                };
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Failed to click: {ex.Message}",
                    Exception = ex
                };
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private void PerformClick(int absoluteX, int absoluteY)
        {
            PerformMouseDown(absoluteX, absoluteY);
            Thread.Sleep(30);
            PerformMouseUp(absoluteX, absoluteY);
        }

        private void PerformMouseDown(int absoluteX, int absoluteY)
        {
            uint downFlag = Button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
                MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
                _ => MOUSEEVENTF_LEFTDOWN
            };
            var input = new INPUT { type = INPUT_MOUSE };
            input.union.mi.dx = absoluteX;
            input.union.mi.dy = absoluteY;
            input.union.mi.dwFlags = downFlag | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void PerformMouseUp(int absoluteX, int absoluteY)
        {
            uint upFlag = Button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTUP,
                MouseButton.Right => MOUSEEVENTF_RIGHTUP,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
                _ => MOUSEEVENTF_LEFTUP
            };
            
            var input = new INPUT { type = INPUT_MOUSE };
            input.union.mi.dx = absoluteX;
            input.union.mi.dy = absoluteY;
            input.union.mi.dwFlags = upFlag | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
