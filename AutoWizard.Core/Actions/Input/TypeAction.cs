using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Actions.Input
{
    /// <summary>
    /// 文字輸入模式
    /// </summary>
    public enum InputMode
    {
        Simulate,  // 模擬按鍵
        Direct     // 直接設置
    }

    /// <summary>
    /// 文字輸入指令
    /// </summary>
    public class TypeAction : BaseAction
    {
        public string Text { get; set; } = string.Empty;
        public InputMode Mode { get; set; } = InputMode.Simulate;
        public int IntervalMinMs { get; set; } = 50;
        public int IntervalMaxMs { get; set; } = 150;

        [DllImport("user32.dll")]
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
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
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
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                // 解析文字中的變數表達式
                string resolvedText = context.ResolveExpression(Text);

                var random = new Random();
                
                foreach (char c in resolvedText)
                {
                    SendUnicodeChar(c);
                    
                    int minDelay = IntervalMinMs;
                    int maxDelay = IntervalMaxMs;

                    if (context.ForceHumanLikeBehavior)
                    {
                        // 若強制人類行為且設定的間隔太快，強制加上合理的人類打字延遲 (50-200ms)
                        minDelay = Math.Max(minDelay, 50);
                        maxDelay = Math.Max(maxDelay, 200);
                    }

                    // 隨機間隔模擬人類輸入
                    int delay = random.Next(minDelay, maxDelay);
                    Thread.Sleep(delay);
                }

                context.Log($"Typed text: {resolvedText.Substring(0, Math.Min(20, resolvedText.Length))}...");

                return new ActionResult
                {
                    Success = true,
                    Message = $"Successfully typed {Text.Length} characters"
                };
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Failed to type: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private void SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            
            // Key down
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].union.ki.wScan = c;
            inputs[0].union.ki.dwFlags = KEYEVENTF_UNICODE;
            
            // Key up
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].union.ki.wScan = c;
            inputs[1].union.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
