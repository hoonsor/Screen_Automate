using System;
using System.Runtime.InteropServices;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Actions.Control
{
    public class WindowAction : BaseAction
    {
        public string TargetWindowTitle { get; set; } = string.Empty;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(TargetWindowTitle))
                {
                    context.TargetWindowHandle = IntPtr.Zero;
                    context.TargetWindowTitle = string.Empty;
                    context.Log("Window target reset to global desktop");
                    return new ActionResult { Success = true, Message = "Reset to global desktop" };
                }

                // 解析可能的變數
                string resolvedTitle = context.ResolveExpression(TargetWindowTitle);

                IntPtr hWnd = FindWindow(null, resolvedTitle);

                if (hWnd != IntPtr.Zero)
                {
                    context.TargetWindowHandle = hWnd;
                    context.TargetWindowTitle = resolvedTitle;
                    
                    context.Log($"Bound execution to window: '{resolvedTitle}' (Handle: {hWnd})");
                    
                    return new ActionResult
                    {
                        Success = true,
                        Message = $"Successfully bound to window '{resolvedTitle}'"
                    };
                }
                else
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = $"Could not find window with title '{resolvedTitle}'"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Failed to bind window: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
