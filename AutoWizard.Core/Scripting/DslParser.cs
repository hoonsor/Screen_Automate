using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Vision;

namespace AutoWizard.Core.Scripting
{
    public class DslParser
    {
        public List<BaseAction> Parse(string script)
        {
            var actions = new List<BaseAction>();
            var lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            Stack<List<BaseAction>> hierarchyStack = new Stack<List<BaseAction>>();
            hierarchyStack.Push(actions);

            string lastComment = string.Empty;
            IfAction? pendingIfAction = null; // 用於收集 .Cond() 鏈式呼叫

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("//"))
                {
                    lastComment = trimmed.Substring(2).Trim();
                    continue;
                }

                // 處理 .Cond(...) 鏈式呼叫行
                if (pendingIfAction != null && trimmed.StartsWith(".Cond("))
                {
                    var condMatch = Regex.Match(trimmed, @"^\.Cond\((.*)\)\s*$");
                    if (condMatch.Success)
                    {
                        var condArgs = ParseArguments(condMatch.Groups[1].Value);
                        var condItem = new ConditionItem
                        {
                            ConditionType = ParseEnum(condArgs, 0, ConditionType.VariableEquals),
                            LeftOperand = ParseString(condArgs, 1),
                            RightOperand = ParseString(condArgs, 2),
                            ConditionExpression = ParseString(condArgs, 3),
                            ColorXExpression = ParseString(condArgs, 4),
                            ColorYExpression = ParseString(condArgs, 5),
                            TargetColor = ParseString(condArgs, 6),
                            Tolerance = ParseInt(condArgs, 7)
                        };
                        pendingIfAction.Conditions.Add(condItem);
                        continue;
                    }
                }

                // 遇到 { 時，如果有 pendingIfAction，將其推入 hierarchy
                if (trimmed == "{")
                {
                    if (pendingIfAction != null)
                    {
                        hierarchyStack.Peek().Add(pendingIfAction);
                        hierarchyStack.Push((List<BaseAction>)pendingIfAction.ThenActions);
                        pendingIfAction = null;
                    }
                    continue;
                }

                if (trimmed == "}") 
                {
                    if (hierarchyStack.Count > 1) hierarchyStack.Pop();
                    continue;
                }
                
                // Parse function call: Name(Args) or Name(Args).ErrorPolicy(...)
                string mainCall = trimmed;
                string errorPolicyStr = string.Empty;

                var epMatch = Regex.Match(trimmed, @"\.ErrorPolicy\(([^)]*)\)\s*;?\s*$");
                if (epMatch.Success)
                {
                    errorPolicyStr = epMatch.Groups[1].Value;
                    mainCall = trimmed.Substring(0, epMatch.Index);
                    mainCall = mainCall.TrimEnd(';').TrimEnd();
                }

                var match = Regex.Match(mainCall, @"^(\w+)\((.*)?)\)\s*;?\s*$");
                if (match.Success)
                {
                    string funcName = match.Groups[1].Value;
                    string argsStr = match.Groups[2].Value;
                    var args = ParseArguments(argsStr);

                    // 新格式 If("ConditionRelation") → 開始收集 .Cond() 行
                    if (funcName == "If" && args.Count == 1 && !Enum.TryParse<ConditionType>(StripQuotes(args[0]), out _))
                    {
                        pendingIfAction = new IfAction
                        {
                            ConditionRelation = ParseString(args, 0),
                            Conditions = new List<ConditionItem>()
                        };
                        if (!string.IsNullOrEmpty(lastComment))
                        {
                            pendingIfAction.Description = lastComment;
                            pendingIfAction.Name = lastComment;
                            lastComment = string.Empty;
                        }
                        if (!string.IsNullOrEmpty(errorPolicyStr))
                        {
                            var epArgs = ParseArguments(errorPolicyStr);
                            pendingIfAction.ErrorPolicy = new ErrorHandlingPolicy
                            {
                                RetryCount = epArgs.Count > 0 && int.TryParse(epArgs[0], out int rc) ? rc : 0,
                                RetryIntervalMs = epArgs.Count > 1 && int.TryParse(epArgs[1], out int ri) ? ri : 1000,
                                ContinueOnError = epArgs.Count > 2 && bool.TryParse(epArgs[2], out bool ce) && ce
                            };
                        }
                        continue;
                    }

                    BaseAction? action = CreateAction(funcName, args);
                    if (action != null)
                    {
                        if (!string.IsNullOrEmpty(errorPolicyStr))
                        {
                            var epArgs = ParseArguments(errorPolicyStr);
                            action.ErrorPolicy = new ErrorHandlingPolicy
                            {
                                RetryCount = epArgs.Count > 0 && int.TryParse(epArgs[0], out int rc) ? rc : 0,
                                RetryIntervalMs = epArgs.Count > 1 && int.TryParse(epArgs[1], out int ri) ? ri : 1000,
                                ContinueOnError = epArgs.Count > 2 && bool.TryParse(epArgs[2], out bool ce) && ce
                            };
                        }

                        if (!string.IsNullOrEmpty(lastComment))
                        {
                            action.Description = lastComment;
                            action.Name = lastComment; 
                        }
                        
                        hierarchyStack.Peek().Add(action);
                        
                        if (action is LoopAction loop)
                        {
                            hierarchyStack.Push((List<BaseAction>)loop.Children);
                        }
                        else if (action is IfAction ifAction)
                        {
                            hierarchyStack.Push((List<BaseAction>)ifAction.ThenActions);
                        }
                    }
                    lastComment = string.Empty;
                }
                else if (trimmed == "Else")
                {
                    var currentList = hierarchyStack.Peek();
                    if (currentList.Count > 0 && currentList[currentList.Count - 1] is IfAction ifAction)
                    {
                        hierarchyStack.Push((List<BaseAction>)ifAction.ElseActions);
                    }
                    lastComment = string.Empty; 
                }
            }

            return actions;
        }

        private List<string> ParseArguments(string argsStr)
        {
            var args = new List<string>();
            var currentArg = new System.Text.StringBuilder();
            bool inQuote = false;
            bool escaped = false;

            foreach (char c in argsStr)
            {
                if (escaped)
                {
                    currentArg.Append(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inQuote = !inQuote;
                }
                else if (c == ',' && !inQuote)
                {
                    args.Add(currentArg.ToString().Trim());
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }
            }
            if (currentArg.Length > 0 || argsStr.Length == 0)
            {
                string last = currentArg.ToString().Trim();
                if (!string.IsNullOrEmpty(last)) args.Add(last);
            }
            return args;
        }

        private string StripQuotes(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input.Substring(1, input.Length - 2);
            }
            return input;
        }

        private BaseAction? CreateAction(string name, List<string> args)
        {
            try
            {
                switch (name)
                {
                    case "Click": return ParseClick(args);
                    case "Type": return ParseType(args);
                    case "Wait": return ParseWait(args);
                    case "Keyboard": return ParseKeyboard(args);
                    case "Screenshot": return ParseScreenshot(args);
                    case "SetVariable": return ParseSetVariable(args);
                    case "FindImage": return ParseFindImage(args);
                    case "OCR": return ParseOCR(args);
                    case "Loop": return ParseLoop(args);
                    case "If": return ParseIf(args);
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private int ParseInt(List<string> args, int index, int defaultVal = 0)
        {
            if (index >= args.Count) return defaultVal;
            if (int.TryParse(args[index], out var val)) return val;
            return defaultVal;
        }
        
        private int? ParseNullableInt(List<string> args, int index)
        {
            if (index >= args.Count) return null;
            if (args[index] == "null") return null;
            if (int.TryParse(args[index], out var val)) return val;
            return null;
        }

        private double ParseDouble(List<string> args, int index, double defaultVal = 0.0)
        {
            if (index >= args.Count) return defaultVal;
            if (double.TryParse(args[index], out var val)) return val;
            return defaultVal;
        }

        private bool ParseBool(List<string> args, int index, bool defaultVal = false)
        {
            if (index >= args.Count) return defaultVal;
            if (bool.TryParse(args[index], out var val)) return val;
            return defaultVal;
        }

        private string ParseString(List<string> args, int index)
        {
            if (index >= args.Count) return string.Empty;
            return StripQuotes(args[index]);
        }

        private T ParseEnum<T>(List<string> args, int index, T defaultVal) where T : struct
        {
            if (index >= args.Count) return defaultVal;
            if (Enum.TryParse<T>(StripQuotes(args[index]), out var val)) return val;
            return defaultVal;
        }

        private ClickAction ParseClick(List<string> args)
        {
            return new ClickAction
            {
                X = ParseInt(args, 0),
                Y = ParseInt(args, 1),
                Button = ParseEnum(args, 2, MouseButton.Left),
                ClickType = ParseEnum(args, 3, ClickType.Single),
                XExpression = ParseString(args, 4),
                YExpression = ParseString(args, 5),
                IsHumanLike = ParseBool(args, 6),
                HumanLikeDurationMs = ParseInt(args, 7, 500)
            };
        }

        private TypeAction ParseType(List<string> args)
        {
            return new TypeAction
            {
                Text = ParseString(args, 0),
                Mode = ParseEnum(args, 1, InputMode.Simulate),
                IntervalMinMs = ParseInt(args, 2, 50),
                IntervalMaxMs = ParseInt(args, 3, 150)
            };
        }

        private WaitAction ParseWait(List<string> args)
        {
            return new WaitAction
            {
                DurationMs = ParseInt(args, 0, 1000),
                WaitType = ParseEnum(args, 1, WaitType.Fixed),
                DurationExpression = ParseString(args, 2),
                RandomMinMs = ParseInt(args, 3, 500),
                RandomMaxMs = ParseInt(args, 4, 2000)
            };
        }

        private KeyboardAction ParseKeyboard(List<string> args)
        {
            return new KeyboardAction
            {
                Key = ParseString(args, 0),
                Modifiers = ParseEnum(args, 1, KeyModifiers.None),
                HoldDurationMs = ParseInt(args, 2)
            };
        }

        private ScreenshotAction ParseScreenshot(List<string> args)
        {
            return new ScreenshotAction
            {
                SavePath = ParseString(args, 0),
                SaveToVariable = ParseString(args, 1),
                CaptureFull = ParseBool(args, 2, true),
                RegionX = ParseInt(args, 3),
                RegionY = ParseInt(args, 4),
                RegionWidth = ParseInt(args, 5),
                RegionHeight = ParseInt(args, 6)
            };
        }

        private SetVariableAction ParseSetVariable(List<string> args)
        {
            return new SetVariableAction
            {
                VariableName = ParseString(args, 0),
                ValueExpression = ParseString(args, 1)
            };
        }

        private FindImageAction ParseFindImage(List<string> args)
        {
            return new FindImageAction
            {
                TemplateImagePath = ParseString(args, 0),
                Threshold = ParseDouble(args, 1, 0.8),
                TimeoutMs = ParseInt(args, 2, 30000),
                IntervalMs = ParseInt(args, 3, 500),
                ClickWhenFound = ParseBool(args, 4),
                SaveToVariable = ParseString(args, 5)
            };
        }

        private OCRAction ParseOCR(List<string> args)
        {
            return new OCRAction
            {
                RegionX = ParseNullableInt(args, 0),
                RegionY = ParseNullableInt(args, 1),
                RegionWidth = ParseNullableInt(args, 2),
                RegionHeight = ParseNullableInt(args, 3),
                SearchText = ParseString(args, 4),
                UseRegex = ParseBool(args, 5),
                SaveToVariable = ParseString(args, 6),
                Language = ParseString(args, 7)
            };
        }

        private LoopAction ParseLoop(List<string> args)
        {
            return new LoopAction
            {
                LoopType = ParseEnum(args, 0, LoopType.Count),
                Count = ParseInt(args, 1),
                WhileCondition = ParseString(args, 2),
                ForeachVariable = ParseString(args, 3)
            };
        }

        /// <summary>
        /// 向下相容：舊格式 If(ConditionType, Left, Right, Expr)
        /// 自動轉換為單條件 + ConditionRelation = "C1"
        /// </summary>
        private IfAction ParseIf(List<string> args)
        {
            var condItem = new ConditionItem
            {
                ConditionType = ParseEnum(args, 0, ConditionType.VariableEquals),
                LeftOperand = ParseString(args, 1),
                RightOperand = ParseString(args, 2),
                ConditionExpression = ParseString(args, 3)
            };
            return new IfAction
            {
                Conditions = new List<ConditionItem> { condItem },
                ConditionRelation = "C1"
            };
        }
    }
}
