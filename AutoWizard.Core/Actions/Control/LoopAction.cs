using System;
using System.Collections.Generic;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Actions.Control
{
    /// <summary>
    /// 迴圈類型
    /// </summary>
    public enum LoopType
    {
        Count,      // 固定次數
        While,      // 條件迴圈
        Foreach     // 列表迭代
    }

    /// <summary>
    /// 迴圈指令
    /// </summary>
    public class LoopAction : ContainerAction
    {
        public LoopType LoopType { get; set; } = LoopType.Count;
        public int Count { get; set; } = 1;
        public string WhileCondition { get; set; } = string.Empty;
        public string ForeachVariable { get; set; } = string.Empty;
        public List<string> ForeachList { get; set; } = new();

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                int iteration = 0;
                int maxIterations = LoopType == LoopType.Count ? Count : 10000; // 安全上限

                while (iteration < maxIterations)
                {
                    // 檢查迴圈條件
                    if (LoopType == LoopType.While && !EvaluateWhileCondition(context))
                    {
                        break;
                    }

                    if (LoopType == LoopType.Foreach && iteration >= ForeachList.Count)
                    {
                        break;
                    }

                    // 設定迴圈變數
                    context.Variables["_loopIndex"] = iteration;
                    
                    if (LoopType == LoopType.Foreach && iteration < ForeachList.Count)
                    {
                        context.Variables[ForeachVariable] = ForeachList[iteration];
                    }

                    context.Log($"Loop iteration {iteration + 1}");

                    // 執行子指令
                    var result = ExecuteChildren(context);
                    
                    if (!result.Success)
                    {
                        return result;
                    }

                    if (context.IsCancellationRequested)
                    {
                        return new ActionResult
                        {
                            Success = false,
                            Message = "Loop cancelled by user"
                        };
                    }

                    iteration++;

                    if (LoopType == LoopType.Count && iteration >= Count)
                    {
                        break;
                    }
                }

                return new ActionResult
                {
                    Success = true,
                    Message = $"Loop completed {iteration} iterations"
                };
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Loop failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private bool EvaluateWhileCondition(Models.ExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(WhileCondition))
                return false;

            return context.EvaluateCondition(WhileCondition);
        }
    }
}
