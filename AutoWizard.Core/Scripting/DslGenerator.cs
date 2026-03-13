using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Vision;

namespace AutoWizard.Core.Scripting
{
    public class DslGenerator
    {
        public string Generate(IEnumerable<BaseAction> actions)
        {
            var sb = new StringBuilder();
            foreach (var action in actions)
            {
                GenerateAction(sb, action, 0);
            }
            return sb.ToString();
        }

        private void GenerateAction(StringBuilder sb, BaseAction action, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);

            // Add comment
            if (!string.IsNullOrEmpty(action.Description))
            {
                sb.AppendLine($"{indent}// {action.Description}");
            }
            else
            {
                // Auto-generate comment if description is empty, based on action type
                sb.AppendLine($"{indent}// {GetDefaultDescription(action)}");
            }

            // Generate function call
            int contentStartPos = sb.Length; // 記錄生成前的位置
            switch (action)
            {
                case ClickAction click:
                    GenerateClick(sb, click, indent);
                    break;
                case TypeAction type:
                    GenerateType(sb, type, indent);
                    break;
                case WaitAction wait:
                    GenerateWait(sb, wait, indent);
                    break;
                case KeyboardAction kb:
                    GenerateKeyboard(sb, kb, indent);
                    break;
                case ScreenshotAction shot:
                    GenerateScreenshot(sb, shot, indent);
                    break;
                case SetVariableAction setVar:
                    GenerateSetVariable(sb, setVar, indent);
                    break;
                case FindImageAction findImg:
                    GenerateFindImage(sb, findImg, indent);
                    break;
                case OCRAction ocr:
                    GenerateOCR(sb, ocr, indent);
                    break;
                case LoopAction loop:
                    GenerateLoop(sb, loop, indent);
                    break;
                case IfAction ifAction:
                    GenerateIf(sb, ifAction, indent);
                    break;
                default:
                    sb.AppendLine($"{indent}// Unsupported action: {action.GetType().Name}");
                    break;
            }

            // 如果 ErrorPolicy 非預設值，附加 .ErrorPolicy(...) 語法
            // 只在本指令新增的首行搜尋分號，避免影響子指令
            AppendErrorPolicyIfNeeded(sb, action, contentStartPos);

            sb.AppendLine(); // Empty line between actions
        }

        private string GetDefaultDescription(BaseAction action)
        {
            return action switch
            {
                ClickAction c => $"Click at ({c.X},{c.Y})",
                TypeAction t => $"Type \"{Truncate(t.Text, 20)}\"",
                WaitAction w => "Wait",
                KeyboardAction k => $"Press Key {k.Key}",
                ScreenshotAction s => "Capture Screenshot",
                SetVariableAction v => $"Set Variable {v.VariableName} = {v.ValueExpression}",
                FindImageAction f => $"Find Image {System.IO.Path.GetFileName(f.TemplateImagePath)}",
                OCRAction o => "OCR Text",
                LoopAction l => "Loop",
                IfAction i => "Condition",
                _ => action.Name ?? action.GetType().Name
            };
        }

        private string Truncate(string s, int max) => s?.Length > max ? s.Substring(0, max) + "..." : s;

        private void GenerateClick(StringBuilder sb, ClickAction action, string indent)
        {
            // Click(X, Y, Button, ClickType, XExpression, YExpression, IsHumanLike, HumanLikeDurationMs)
            var args = new List<string>
            {
                action.X.ToString(),
                action.Y.ToString(),
                $"\"{action.Button}\"",
                $"\"{action.ClickType}\"",
                $"\"{EscapeString(action.XExpression)}\"",
                $"\"{EscapeString(action.YExpression)}\"",
                action.IsHumanLike.ToString().ToLower(),
                action.HumanLikeDurationMs.ToString()
            };
            sb.AppendLine($"{indent}Click({string.Join(", ", args)});");
        }

        private void GenerateType(StringBuilder sb, TypeAction action, string indent)
        {
            // Type(Text, Mode, IntervalMinMs, IntervalMaxMs)
            var args = new List<string>
            {
                $"\"{EscapeString(action.Text)}\"",
                $"\"{action.Mode}\"",
                action.IntervalMinMs.ToString(),
                action.IntervalMaxMs.ToString()
            };
            sb.AppendLine($"{indent}Type({string.Join(", ", args)});");
        }

        private void GenerateWait(StringBuilder sb, WaitAction action, string indent)
        {
            // Wait(DurationMs, WaitType, DurationExpression, RandomMinMs, RandomMaxMs)
            var args = new List<string>
            {
                action.DurationMs.ToString(),
                $"\"{action.WaitType}\"",
                $"\"{EscapeString(action.DurationExpression)}\"",
                action.RandomMinMs.ToString(),
                action.RandomMaxMs.ToString()
            };
            sb.AppendLine($"{indent}Wait({string.Join(", ", args)});");
        }

        private void GenerateKeyboard(StringBuilder sb, KeyboardAction action, string indent)
        {
            // Keyboard(Key, Modifiers, HoldDurationMs)
            var args = new List<string>
            {
                $"\"{EscapeString(action.Key)}\"",
                $"\"{action.Modifiers}\"",
                action.HoldDurationMs.ToString()
            };
            sb.AppendLine($"{indent}Keyboard({string.Join(", ", args)});");
        }

        private void GenerateScreenshot(StringBuilder sb, ScreenshotAction action, string indent)
        {
            // Screenshot(SavePath, SaveToVariable, CaptureFull, RegionX, RegionY, RegionWidth, RegionHeight)
            var args = new List<string>
            {
                $"\"{EscapeString(action.SavePath)}\"",
                $"\"{EscapeString(action.SaveToVariable)}\"",
                action.CaptureFull.ToString().ToLower(),
                action.RegionX.ToString(),
                action.RegionY.ToString(),
                action.RegionWidth.ToString(),
                action.RegionHeight.ToString()
            };
            sb.AppendLine($"{indent}Screenshot({string.Join(", ", args)});");
        }

        private void GenerateSetVariable(StringBuilder sb, SetVariableAction action, string indent)
        {
            // SetVariable(VariableName, ValueExpression)
            var args = new List<string>
            {
                $"\"{EscapeString(action.VariableName)}\"",
                $"\"{EscapeString(action.ValueExpression)}\""
            };
            sb.AppendLine($"{indent}SetVariable({string.Join(", ", args)});");
        }

        private void GenerateFindImage(StringBuilder sb, FindImageAction action, string indent)
        {
            // FindImage(TemplateImagePath, Threshold, TimeoutMs, IntervalMs, ClickWhenFound, SaveToVariable)
            var args = new List<string>
            {
                $"\"{EscapeString(action.TemplateImagePath)}\"",
                action.Threshold.ToString(),
                action.TimeoutMs.ToString(),
                action.IntervalMs.ToString(),
                action.ClickWhenFound.ToString().ToLower(),
                $"\"{EscapeString(action.SaveToVariable)}\""
            };
            sb.AppendLine($"{indent}FindImage({string.Join(", ", args)});");
        }

        private void GenerateOCR(StringBuilder sb, OCRAction action, string indent)
        {
            // OCR(RegionX, RegionY, RegionWidth, RegionHeight, SearchText, UseRegex, SaveToVariable, Language)
            var args = new List<string>
            {
                AsString(action.RegionX),
                AsString(action.RegionY),
                AsString(action.RegionWidth),
                AsString(action.RegionHeight),
                $"\"{EscapeString(action.SearchText)}\"",
                action.UseRegex.ToString().ToLower(),
                $"\"{EscapeString(action.SaveToVariable)}\"",
                $"\"{EscapeString(action.Language)}\""
            };
            sb.AppendLine($"{indent}OCR({string.Join(", ", args)});");
        }

        private string AsString(int? val) => val.HasValue ? val.Value.ToString() : "null";

        private void GenerateLoop(StringBuilder sb, LoopAction action, string indent)
        {
            // Loop(LoopType, Count, WhileCondition, ForeachVariable)
            var args = new List<string>
            {
                $"\"{action.LoopType}\"",
                action.Count.ToString(),
                $"\"{EscapeString(action.WhileCondition)}\"",
                $"\"{EscapeString(action.ForeachVariable)}\""
            };
            sb.AppendLine($"{indent}Loop({string.Join(", ", args)})");
            sb.AppendLine($"{indent}{{");
            foreach (var child in action.Children)
            {
                GenerateAction(sb, child, indent.Length / 4 + 1);
            }
            sb.AppendLine($"{indent}}}");
        }

        private void GenerateIf(StringBuilder sb, IfAction action, string indent)
        {
            // If("ConditionRelation")
            //   .Cond("CondType", "Left", "Right", "Expr", "ColorX", "ColorY", "TargetColor", Tolerance)
            //   .Cond(...)
            // { ... }
            sb.AppendLine($"{indent}If(\"{EscapeString(action.ConditionRelation)}\")");

            foreach (var cond in action.Conditions)
            {
                var condArgs = new List<string>
                {
                    $"\"{cond.ConditionType}\"",
                    $"\"{EscapeString(cond.LeftOperand)}\"",
                    $"\"{EscapeString(cond.RightOperand)}\"",
                    $"\"{EscapeString(cond.ConditionExpression)}\"",
                    $"\"{EscapeString(cond.ColorXExpression)}\"",
                    $"\"{EscapeString(cond.ColorYExpression)}\"",
                    $"\"{EscapeString(cond.TargetColor)}\"",
                    cond.Tolerance.ToString()
                };
                sb.AppendLine($"{indent}  .Cond({string.Join(", ", condArgs)})");
            }

            sb.AppendLine($"{indent}{{");
            foreach (var child in action.ThenActions)
            {
                GenerateAction(sb, child, indent.Length / 4 + 1);
            }
            sb.AppendLine($"{indent}}}");
            
            if (action.ElseActions.Any())
            {
                sb.AppendLine($"{indent}Else");
                sb.AppendLine($"{indent}{{");
                foreach (var child in action.ElseActions)
                {
                    GenerateAction(sb, child, indent.Length / 4 + 1);
                }
                sb.AppendLine($"{indent}}}");
            }
        }

        private string EscapeString(string input)
        {
            if (input == null) return "";
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        /// <summary>
        /// 檢查 ErrorPolicy 是否為非預設值，若是則在指令行後附加 .ErrorPolicy(...) 語法
        /// contentStartPos 用來限定只搜尋本指令新增的首行，避免影響子指令
        /// </summary>
        private void AppendErrorPolicyIfNeeded(StringBuilder sb, BaseAction action, int contentStartPos)
        {
            var ep = action.ErrorPolicy;
            bool isDefault = (ep.RetryCount == 0 && ep.RetryIntervalMs == 1000 && !ep.ContinueOnError);

            if (!isDefault)
            {
                string content = sb.ToString();
                // 只在本指令新增的內容中搜尋首行的分號
                int searchEnd = content.IndexOf('\n', contentStartPos);
                if (searchEnd < 0) searchEnd = content.Length;

                int semiPos = content.IndexOf(';', contentStartPos);
                if (semiPos >= 0 && semiPos < searchEnd)
                {
                    string errorPolicyCall = $".ErrorPolicy({ep.RetryCount}, {ep.RetryIntervalMs}, {ep.ContinueOnError.ToString().ToLower()})";
                    sb.Clear();
                    sb.Append(content.Substring(0, semiPos));
                    sb.Append(errorPolicyCall);
                    sb.Append(content.Substring(semiPos));
                }
            }
        }
    }
}
