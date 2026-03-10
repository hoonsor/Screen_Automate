using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoWizard.Core.Engine
{
    /// <summary>
    /// 集中式表達式解析器
    /// 支援變數替換 {varName}、算術運算、布林條件
    /// </summary>
    public static class ExpressionParser
    {
        // 匹配 {varName} 模式
        private static readonly Regex VarPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

        /// <summary>
        /// 將字串中所有 {varName} 替換為實際值
        /// 例: "Hello {name}, count={count}" → "Hello World, count=5"
        /// </summary>
        public static string Resolve(string input, Dictionary<string, object> variables)
        {
            if (string.IsNullOrEmpty(input) || variables == null)
                return input ?? string.Empty;

            return VarPattern.Replace(input, match =>
            {
                string varName = match.Groups[1].Value;
                if (variables.TryGetValue(varName, out var value))
                {
                    string strValue = value?.ToString() ?? string.Empty;

                    // Dynamic color check hook
                    if (varName.StartsWith("color_check_") && !string.IsNullOrWhiteSpace(strValue))
                    {
                        return EvaluateDynamicColorCheck(strValue) ? "True" : "False";
                    }

                    return strValue;
                }
                // 找不到變數，保留原始文字
                return match.Value;
            });
        }

        private static bool EvaluateDynamicColorCheck(string configValue)
        {
            try
            {
                // Expected format: X,Y,#HEX,Tolerance
                var parts = configValue.Split(',');
                if (parts.Length < 3) return false;

                if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    return false;

                string targetHex = parts[2];
                int tolerance = parts.Length > 3 && int.TryParse(parts[3], out int tol) ? tol : 0;

                var targetColor = System.Drawing.ColorTranslator.FromHtml(targetHex);

                using var bmp = new System.Drawing.Bitmap(1, 1);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                var actualColor = bmp.GetPixel(0, 0);

                int rDiff = Math.Abs(targetColor.R - actualColor.R);
                int gDiff = Math.Abs(targetColor.G - actualColor.G);
                int bDiff = Math.Abs(targetColor.B - actualColor.B);

                return (rDiff <= tolerance && gDiff <= tolerance && bDiff <= tolerance);
            }
            catch
            {
                return false; // Silently fail on error, e.g invalid coords or hex
            }
        }

        /// <summary>
        /// 嘗試將字串解析為整數（先做變數替換）
        /// </summary>
        public static int ResolveInt(string input, Dictionary<string, object> variables, int fallback = 0)
        {
            string resolved = Resolve(input, variables);
            return TryEvaluateNumeric(resolved, out double result) ? (int)result : fallback;
        }

        /// <summary>
        /// 嘗試將字串解析為浮點數（先做變數替換）
        /// </summary>
        public static double ResolveDouble(string input, Dictionary<string, object> variables, double fallback = 0.0)
        {
            string resolved = Resolve(input, variables);
            return TryEvaluateNumeric(resolved, out double result) ? result : fallback;
        }

        /// <summary>
        /// 計算算術表達式，回傳數值結果
        /// 支援: +, -, *, /, % 以及括號
        /// 例: "10 + 5 * 2" → 20.0
        /// </summary>
        public static double Evaluate(string expression, Dictionary<string, object> variables)
        {
            // 先替換變數
            string resolved = Resolve(expression, variables);

            if (TryEvaluateNumeric(resolved, out double result))
                return result;

            throw new FormatException($"Cannot evaluate expression: '{expression}' (resolved: '{resolved}')");
        }

        /// <summary>
        /// 嘗試計算表達式，不拋出例外
        /// </summary>
        public static bool TryEvaluateExpression(string expression, out double result)
        {
            return TryEvaluateNumeric(expression, out result);
        }

        /// <summary>
        /// 計算布林條件表達式
        /// 支援: ==, !=, >, <, >=, <=, contains
        /// 例: "{count} > 0", "{name} == admin", "{text} contains hello"
        /// </summary>
        public static bool EvaluateCondition(string expression, Dictionary<string, object> variables)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            // 先替換變數
            string resolved = Resolve(expression, variables);

            // 嘗試比較運算子 (有序 — >= 和 <= 必須在 > 和 < 之前)
            string[] operators = { ">=", "<=", "!=", "==", ">", "<", " contains " };

            foreach (var op in operators)
            {
                int idx = resolved.IndexOf(op, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string left = resolved.Substring(0, idx).Trim();
                    string right = resolved.Substring(idx + op.Length).Trim();
                    return EvaluateComparison(left, op.Trim(), right);
                }
            }

            // 無運算子 — 嘗試將整個字串視為布林值
            if (bool.TryParse(resolved, out bool boolResult))
                return boolResult;

            // 非空字串視為 true
            return !string.IsNullOrWhiteSpace(resolved) && resolved != "0";
        }

        #region Internal Helpers

        /// <summary>
        /// 嘗試計算簡單的算術表達式
        /// </summary>
        private static bool TryEvaluateNumeric(string expr, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(expr))
                return false;

            expr = expr.Trim();

            // 純數字
            if (double.TryParse(expr, out result))
                return true;

            // 嘗試簡單的二元運算: "a op b"
            // 支援 +, -, *, /, %
            var match = Regex.Match(expr, @"^(-?\d+(?:\.\d+)?)\s*([+\-*/%])\s*(-?\d+(?:\.\d+)?)$");
            if (match.Success)
            {
                double left = double.Parse(match.Groups[1].Value);
                string op = match.Groups[2].Value;
                double right = double.Parse(match.Groups[3].Value);

                result = op switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => right != 0 ? left / right : 0,
                    "%" => right != 0 ? left % right : 0,
                    _ => 0
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 執行比較運算
        /// </summary>
        private static bool EvaluateComparison(string left, string op, string right)
        {
            // 嘗試數值比較
            bool leftIsNum = double.TryParse(left, out double leftNum);
            bool rightIsNum = double.TryParse(right, out double rightNum);

            return op switch
            {
                "==" => leftIsNum && rightIsNum ? leftNum == rightNum : left == right,
                "!=" => leftIsNum && rightIsNum ? leftNum != rightNum : left != right,
                ">" => leftIsNum && rightIsNum ? leftNum > rightNum : string.Compare(left, right, StringComparison.Ordinal) > 0,
                "<" => leftIsNum && rightIsNum ? leftNum < rightNum : string.Compare(left, right, StringComparison.Ordinal) < 0,
                ">=" => leftIsNum && rightIsNum ? leftNum >= rightNum : string.Compare(left, right, StringComparison.Ordinal) >= 0,
                "<=" => leftIsNum && rightIsNum ? leftNum <= rightNum : string.Compare(left, right, StringComparison.Ordinal) <= 0,
                "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        #endregion
    }
}
