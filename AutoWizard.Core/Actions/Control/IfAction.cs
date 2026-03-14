using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using AutoWizard.Core.Models;
using AutoWizard.CV.Vision;
using AutoWizard.CV.Capture;

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
    /// If-Else 條件判斷指令，支援多條件組合
    /// </summary>
    public class IfAction : ContainerAction
    {
        /// <summary>
        /// 多條件列表，每個元素為一個獨立條件
        /// </summary>
        public List<ConditionItem> Conditions { get; set; } = new() { new ConditionItem() };

        /// <summary>
        /// 條件關係表達式，如 "C1", "C1 && C2", "(C1 && C2) || C3"
        /// 預設為 "C1"（單條件）
        /// </summary>
        public string ConditionRelation { get; set; } = "C1";

        // ===== 向下相容屬性 =====
        // 這些屬性映射到 Conditions[0]，供舊版序列化/反序列化使用

        public ConditionType ConditionType
        {
            get => Conditions.Count > 0 ? Conditions[0].ConditionType : ConditionType.VariableEquals;
            set { EnsureFirstCondition(); Conditions[0].ConditionType = value; }
        }

        public string LeftOperand
        {
            get => Conditions.Count > 0 ? Conditions[0].LeftOperand : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].LeftOperand = value; }
        }

        public string RightOperand
        {
            get => Conditions.Count > 0 ? Conditions[0].RightOperand : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].RightOperand = value; }
        }

        public string ColorXExpression
        {
            get => Conditions.Count > 0 ? Conditions[0].ColorXExpression : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].ColorXExpression = value; }
        }

        public string ColorYExpression
        {
            get => Conditions.Count > 0 ? Conditions[0].ColorYExpression : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].ColorYExpression = value; }
        }

        public string TargetColor
        {
            get => Conditions.Count > 0 ? Conditions[0].TargetColor : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].TargetColor = value; }
        }

        public int Tolerance
        {
            get => Conditions.Count > 0 ? Conditions[0].Tolerance : 0;
            set { EnsureFirstCondition(); Conditions[0].Tolerance = value; }
        }

        /// <summary>
        /// 自由格式條件表達式（用於 ConditionType.Expression）
        /// 例: "{count} > 0", "{name} == admin"
        /// </summary>
        public string ConditionExpression
        {
            get => Conditions.Count > 0 ? Conditions[0].ConditionExpression : string.Empty;
            set { EnsureFirstCondition(); Conditions[0].ConditionExpression = value; }
        }

        public List<BaseAction> ThenActions { get; set; } = new();
        public List<BaseAction> ElseActions { get; set; } = new();

        private void EnsureFirstCondition()
        {
            if (Conditions.Count == 0)
                Conditions.Add(new ConditionItem());
        }

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                bool conditionMet = EvaluateAllConditions(context);

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

        /// <summary>
        /// 評估所有條件並根據 ConditionRelation 組合結果
        /// </summary>
        private bool EvaluateAllConditions(Models.ExecutionContext context)
        {
            // 單條件快速路徑
            if (Conditions.Count == 1)
            {
                return EvaluateSingleCondition(Conditions[0], context);
            }

            // 評估每個條件的結果
            var results = new Dictionary<string, bool>();
            for (int i = 0; i < Conditions.Count; i++)
            {
                string key = $"C{i + 1}";
                results[key] = EvaluateSingleCondition(Conditions[i], context);
                context.Log($"  {key} = {results[key]}");
            }

            // 解析 ConditionRelation 表達式
            return EvaluateRelation(ConditionRelation, results);
        }

        /// <summary>
        /// 評估單一條件
        /// </summary>
        internal bool EvaluateSingleCondition(ConditionItem condition, Models.ExecutionContext context)
        {
            // Expression 模式
            if (condition.ConditionType == ConditionType.Expression)
            {
                return context.EvaluateCondition(condition.ConditionExpression);
            }

            // ColorMatch 模式
            if (condition.ConditionType == ConditionType.ColorMatch)
            {
                return EvaluateColorMatch(condition, context);
            }

            // ImageExists 模式
            if (condition.ConditionType == ConditionType.ImageExists)
            {
                return EvaluateImageExists(condition, context);
            }

            // 傳統模式：解析左右運算元
            string left = context.ResolveExpression(condition.LeftOperand);
            string right = context.ResolveExpression(condition.RightOperand);

            return condition.ConditionType switch
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

        private bool EvaluateColorMatch(ConditionItem condition, Models.ExecutionContext context)
        {
            try
            {
                string xStr = context.ResolveExpression(condition.ColorXExpression);
                string yStr = context.ResolveExpression(condition.ColorYExpression);
                string targetColorStr = context.ResolveExpression(condition.TargetColor);

                if (!int.TryParse(xStr, out int x) || !int.TryParse(yStr, out int y))
                {
                    context.Log($"ColorMatch failed: Invalid coordinates ({xStr}, {yStr})");
                    return false;
                }

                var targetColor = System.Drawing.ColorTranslator.FromHtml(targetColorStr);

                using var bmp = new System.Drawing.Bitmap(1, 1);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                var actualColor = bmp.GetPixel(0, 0);

                context.Log($"ColorMatch checking ({x},{y}). Target: {targetColorStr}, Actual: #{actualColor.R:X2}{actualColor.G:X2}{actualColor.B:X2}");

                int rDiff = Math.Abs(targetColor.R - actualColor.R);
                int gDiff = Math.Abs(targetColor.G - actualColor.G);
                int bDiff = Math.Abs(targetColor.B - actualColor.B);

                return rDiff <= condition.Tolerance && gDiff <= condition.Tolerance && bDiff <= condition.Tolerance;
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

        /// <summary>
        /// 評估影像是否存在於畫面上
        /// </summary>
        private bool EvaluateImageExists(ConditionItem condition, Models.ExecutionContext context)
        {
            try
            {
                string imagePath = context.ResolveExpression(condition.LeftOperand);
                double threshold = 0.8;
                if (!string.IsNullOrEmpty(condition.RightOperand))
                {
                    string threshStr = context.ResolveExpression(condition.RightOperand);
                    if (double.TryParse(threshStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                        threshold = parsed;
                }

                if (!System.IO.Path.IsPathRooted(imagePath))
                {
                    string baseDir = context.Variables.TryGetValue("ScriptDirectory", out var dirObj) && dirObj is string dir
                        ? dir
                        : AppDomain.CurrentDomain.BaseDirectory;

                    imagePath = System.IO.Path.Combine(baseDir, imagePath);
                }

                if (!System.IO.File.Exists(imagePath))
                {
                    context.Log($"ImageExists: Template image not found: {imagePath}");
                    return false;
                }

                using var template = new Bitmap(imagePath);
                var result = ImageMatcher.WaitForImage(template, threshold, 0, 100);

                context.Log($"ImageExists: Found={result.Found}, Confidence={result.Confidence:P0}");

                context.Variables["ImageExists_X"] = result.X;
                context.Variables["ImageExists_Y"] = result.Y;
                context.Variables["ImageExists_Confidence"] = result.Confidence;

                return result.Found;
            }
            catch (Exception ex)
            {
                context.Log($"ImageExists evaluation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析條件關係表達式，支援 &&, ||, !, 括號
        /// 例: "C1 && C2", "(C1 || C2) && C3", "!C1"
        /// </summary>
        internal static bool EvaluateRelation(string relation, Dictionary<string, bool> results)
        {
            if (string.IsNullOrWhiteSpace(relation))
                return results.ContainsKey("C1") && results["C1"];

            // 替換條件引用為 True/False
            string expr = relation;
            // 按 C 編號降序替換，避免 C1 匹配 C10 中的前綴
            var sortedKeys = new List<string>(results.Keys);
            sortedKeys.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (var key in sortedKeys)
            {
                expr = expr.Replace(key, results[key] ? "T" : "F");
            }

            // 遞迴解析布林表達式
            return ParseBoolExpression(expr.Replace(" ", ""), 0, out _);
        }

        /// <summary>
        /// 簡易遞迴下降布林表達式解析器
        /// 支援: T, F, !, &&, ||, ()
        /// </summary>
        private static bool ParseBoolExpression(string expr, int pos, out int newPos)
        {
            bool result = ParseBoolOr(expr, pos, out newPos);
            return result;
        }

        private static bool ParseBoolOr(string expr, int pos, out int newPos)
        {
            bool left = ParseBoolAnd(expr, pos, out newPos);
            while (newPos < expr.Length - 1 && expr[newPos] == '|' && expr[newPos + 1] == '|')
            {
                newPos += 2;
                bool right = ParseBoolAnd(expr, newPos, out newPos);
                left = left || right;
            }
            return left;
        }

        private static bool ParseBoolAnd(string expr, int pos, out int newPos)
        {
            bool left = ParseBoolUnary(expr, pos, out newPos);
            while (newPos < expr.Length - 1 && expr[newPos] == '&' && expr[newPos + 1] == '&')
            {
                newPos += 2;
                bool right = ParseBoolUnary(expr, newPos, out newPos);
                left = left && right;
            }
            return left;
        }

        private static bool ParseBoolUnary(string expr, int pos, out int newPos)
        {
            if (pos < expr.Length && expr[pos] == '!')
            {
                bool val = ParseBoolPrimary(expr, pos + 1, out newPos);
                return !val;
            }
            return ParseBoolPrimary(expr, pos, out newPos);
        }

        private static bool ParseBoolPrimary(string expr, int pos, out int newPos)
        {
            if (pos < expr.Length && expr[pos] == '(')
            {
                bool val = ParseBoolExpression(expr, pos + 1, out newPos);
                if (newPos < expr.Length && expr[newPos] == ')')
                    newPos++;
                return val;
            }

            if (pos < expr.Length && expr[pos] == 'T')
            {
                newPos = pos + 1;
                return true;
            }
            if (pos < expr.Length && expr[pos] == 'F')
            {
                newPos = pos + 1;
                return false;
            }

            // 未預期的字元，回傳 false
            newPos = pos + 1;
            return false;
        }
    }
}
