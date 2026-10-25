using System;
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
    /// 文字輸入指令（透過 context.Input 送出，測試時不會真的打字）
    /// </summary>
    public class TypeAction : BaseAction
    {
        public string Text { get; set; } = string.Empty;
        public InputMode Mode { get; set; } = InputMode.Simulate;
        public int IntervalMinMs { get; set; } = 50;
        public int IntervalMaxMs { get; set; } = 150;

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                // 解析文字中的變數表達式
                string resolvedText = context.ResolveExpression(Text);

                var random = new Random();

                foreach (char c in resolvedText)
                {
                    context.Input.SendUnicodeChar(c);

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
    }
}
