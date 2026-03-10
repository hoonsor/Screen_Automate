using System;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Actions.Control
{
    /// <summary>
    /// 條件判斷類型
    /// </summary>
    public enum ConditionType
    {
        VariableEquals,
        VariableNotEquals,
        VariableGreaterThan,
        VariableLessThan,
        VariableContains,
        ImageExists,
        FileExists,
        Expression,    // 使用 ExpressionParser 計算自由格式條件
        ColorMatch     // 比對畫面特定座標的顏色
    }

    /// <summary>
    /// If-Else 條件判斷指令
    /// </summary>
    public class IfAction : ContainerAction
    {
        public ConditionType ConditionType { get; set; }
        public string LeftOperand { get; set; } = string.Empty;
        public string RightOperand { get; set; } = string.Empty;
        
        /// <summary>
        /// 畫面座標 X，可用變數 (供 ColorMatch 使用)
        /// </summary>
        public string ColorXExpression { get; set; } = string.Empty;
        
        /// <summary>
        /// 畫面座標 Y，可用變數 (供 ColorMatch 使用)
        /// </summary>
        public string ColorYExpression { get; set; } = string.Empty;
        
        /// <summary>
        /// 目標顏色，例如 "#FFFFFF" 或 "{color_1}" (供 ColorMatch 使用)
        /// </summary>
        public string TargetColor { get; set; } = string.Empty;
        
        /// <summary>
        /// 色彩容差值 (0~255)
        /// </summary>
        public int Tolerance { get; set; } = 0;

        /// <summary>
        /// 自由格式條件表達式（用於 ConditionType.Expression）
        /// 例: "{count} > 0", "{name} == admin"
        /// </summary>
        public string ConditionExpression { get; set; } = string.Empty;
        public List<BaseAction> ThenActions { get; set; } = new();
        public List<BaseAction> ElseActions { get; set; } = new();

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                bool conditionMet = EvaluateCondition(context);

                context.Log($"Condition evaluated: {conditionMet}");

                // 執行對應分支
                var actionsToExecute = conditionMet ? ThenActions : ElseActions;
                
                foreach (var action in actionsToExecute)
                {
                    if (context.IsCancellationRequested)
                    {
                        return new ActionResult
                        {
                            Success = false,
                            Message = "Execution cancelled"
                        };
                    }

                    var result = action.ExecuteWithPolicy(context);
                    
                    if (!result.Success && !action.ErrorPolicy.ContinueOnError)
                    {
                        return result;
                    }
                }

                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"If condition failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private bool EvaluateCondition(Models.ExecutionContext context)
        {
            // Expression 模式：使用 ExpressionParser 直接計算
            if (ConditionType == ConditionType.Expression)
            {
                return context.EvaluateCondition(ConditionExpression);
            }

            // 特殊模式：顏色比對
            if (ConditionType == ConditionType.ColorMatch)
            {
                return EvaluateColorMatch(context);
            }

            // 傳統模式：解析左右運算元
            string left = context.ResolveExpression(LeftOperand);
            string right = context.ResolveExpression(RightOperand);

            return ConditionType switch
            {
                ConditionType.VariableEquals => left == right,
                ConditionType.VariableNotEquals => left != right,
                ConditionType.VariableContains => left.Contains(right),
                ConditionType.VariableGreaterThan => CompareNumeric(left, right) > 0,
                ConditionType.VariableLessThan => CompareNumeric(left, right) < 0,
                ConditionType.FileExists => System.IO.File.Exists(left),
                _ => false
            };
        }

        private bool EvaluateColorMatch(Models.ExecutionContext context)
        {
            try
            {
                string xStr = context.ResolveExpression(ColorXExpression);
                string yStr = context.ResolveExpression(ColorYExpression);
                string targetColorStr = context.ResolveExpression(TargetColor);

                if (!int.TryParse(xStr, out int x) || !int.TryParse(yStr, out int y))
                {
                    context.Log($"ColorMatch failed: Invalid coordinates ({xStr}, {yStr})");
                    return false;
                }

                // Parse TargetColor (#RRGGBB)
                var targetColor = System.Drawing.ColorTranslator.FromHtml(targetColorStr);

                // Fetch current pixel color
                using var bmp = new System.Drawing.Bitmap(1, 1);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                var actualColor = bmp.GetPixel(0, 0);

                context.Log($"ColorMatch checking ({x},{y}). Target: {targetColorStr}, Actual: #{actualColor.R:X2}{actualColor.G:X2}{actualColor.B:X2}");

                // Check tolerance
                int rDiff = Math.Abs(targetColor.R - actualColor.R);
                int gDiff = Math.Abs(targetColor.G - actualColor.G);
                int bDiff = Math.Abs(targetColor.B - actualColor.B);

                if (rDiff <= Tolerance && gDiff <= Tolerance && bDiff <= Tolerance)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                context.Log($"ColorMatch evaluation error: {ex.Message}");
                return false;
            }
        }

        private static int CompareNumeric(string left, string right)
        {
            if (double.TryParse(left, out double leftNum) && 
                double.TryParse(right, out double rightNum))
            {
                return leftNum.CompareTo(rightNum);
            }
            return string.Compare(left, right, StringComparison.Ordinal);
        }
    }
}
