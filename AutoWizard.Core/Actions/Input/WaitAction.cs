using System;
using System.Threading;

namespace AutoWizard.Core.Actions.Input
{
    public enum WaitType
    {
        Fixed,
        Random
    }

    public class WaitAction : AutoWizard.Core.Models.BaseAction
    {
        public WaitType WaitType { get; set; } = WaitType.Fixed;
        public string DurationExpression { get; set; } = string.Empty;
        public int DurationMs { get; set; } = 1000;
        public int RandomMinMs { get; set; } = 500;
        public int RandomMaxMs { get; set; } = 2000;

        public override AutoWizard.Core.Models.ActionResult Execute(AutoWizard.Core.Models.ExecutionContext context)
        {
            try
            {
                int delay;

                if (WaitType == WaitType.Random)
                {
                    var rng = new Random();
                    delay = rng.Next(RandomMinMs, RandomMaxMs + 1);
                }
                else
                {
                    // 支援表達式解析
                    delay = !string.IsNullOrEmpty(DurationExpression)
                        ? context.ResolveInt(DurationExpression, DurationMs)
                        : DurationMs;
                }

                context.Log($"Waiting {delay}ms...");
                Thread.Sleep(delay);

                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = true,
                    Message = $"Waited {delay}ms"
                };
            }
            catch (Exception ex)
            {
                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = false,
                    Message = $"Wait failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
