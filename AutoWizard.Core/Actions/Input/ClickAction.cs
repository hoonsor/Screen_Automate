using System;
using System.Runtime.InteropServices;
using System.Threading;
using AutoWizard.Core.Models;

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
    /// 滑鼠點擊指令（透過 context.Input 送出，測試時不會真的移動滑鼠或點擊）
    /// </summary>
    public class ClickAction : BaseAction
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; } = MouseButton.Left;
        public ClickType ClickType { get; set; } = ClickType.Single;

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

                var input = context.Input;
                bool useHumanLike = IsHumanLike || context.ForceHumanLikeBehavior;

                // 移動滑鼠到目標位置
                if (useHumanLike)
                {
                    var start = input.GetCursorPosition();
                    MouseMovementHelper.MoveMouseSmoothly(input, start.X, start.Y, resolvedX, resolvedY, HumanLikeDurationMs);
                }
                else
                {
                    // 絕對座標移動，對拖曳和遊戲相容性較好，不會打斷 Down 狀態
                    input.MoveMouse(resolvedX, resolvedY);
                    Thread.Sleep(50); // 短暫延遲確保移動完成
                }

                // 執行點擊
                switch (ClickType)
                {
                    case ClickType.Single:
                        PerformClick(input, resolvedX, resolvedY);
                        break;
                    case ClickType.Double:
                        PerformClick(input, resolvedX, resolvedY);
                        Thread.Sleep(50);
                        PerformClick(input, resolvedX, resolvedY);
                        break;
                    case ClickType.Down:
                        input.MouseButton(Button, true, resolvedX, resolvedY);
                        break;
                    case ClickType.Up:
                        input.MouseButton(Button, false, resolvedX, resolvedY);
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

        private void PerformClick(AutoWizard.Core.Input.IInputSimulator input, int x, int y)
        {
            input.MouseButton(Button, true, x, y);
            Thread.Sleep(30);
            input.MouseButton(Button, false, x, y);
        }
    }
}
