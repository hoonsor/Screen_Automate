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

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("//"))
                {
                    // Store comment (remove "//" and trim)
                    lastComment = trimmed.Substring(2).Trim();
                    continue;
                }

                if (trimmed == "{") continue;
                if (trimmed == "}") 
                {
                    if (hierarchyStack.Count > 1) hierarchyStack.Pop();
                    continue;
                }
                
                // Parse function call: Name(Args)
                var match = Regex.Match(trimmed, @"^(\w+)\((.*)\);?$");
                if (match.Success)
                {
                    string funcName = match.Groups[1].Value;
                    string argsStr = match.Groups[2].Value;
                    var args = ParseArguments(argsStr);

                    BaseAction? action = CreateAction(funcName, args);
                    if (action != null)
                    {
                        // Restore description from comment
                        if (!string.IsNullOrEmpty(lastComment))
                        {
                            action.Description = lastComment;
                            // Also set Name to Description for better visibility if needed, 
                            // or keep default Name. Let's set Name = Description for consistency with Visual Editor behavior?
                            // Usually Name is "Click" or "Type". The User sets "Description" in the UI often as the "Name" they see?
                            // Let's stick to Description. 
                            // If the UI binds to Name and Name is empty, that's bad. 
                            // BaseAction typically defaults Name to class name.
                            // But if the user wants custom text in the list, they usually edit Description.
                            // Let's set Name = Description so the list isn't empty if the DataTemplate uses Name.
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
                    // Reset comment after using it
                    lastComment = string.Empty;
                }
                else if (trimmed == "Else")
                {
                    var currentList = hierarchyStack.Peek();
                    if (currentList.Count > 0 && currentList[currentList.Count - 1] is IfAction ifAction)
                    {
                        hierarchyStack.Push((List<BaseAction>)ifAction.ElseActions);
                    }
                    // Else doesn't consume comment usually, but if there was one, clear it or attach?
                    // Usually Else doesn't have a specific description attached to the line.
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
            if (currentArg.Length > 0 || argsStr.Length == 0) // Handle empty args case or last arg
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
                // Simple stripping, real unescaping happens logic might be needed if I escaped heavily in generator
                // Generator used simple Replace.
                // Here we just remove wrapper.
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
                // Identify parsing errors?
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
            // Loop(LoopType, Count, WhileCondition, ForeachVariable)
            return new LoopAction
            {
                LoopType = ParseEnum(args, 0, LoopType.Count),
                Count = ParseInt(args, 1),
                WhileCondition = ParseString(args, 2),
                ForeachVariable = ParseString(args, 3)
            };
        }

        private IfAction ParseIf(List<string> args)
        {
            // If(ConditionType, Left, Right, Expr)
            return new IfAction
            {
                ConditionType = ParseEnum(args, 0, ConditionType.VariableEquals),
                LeftOperand = ParseString(args, 1),
                RightOperand = ParseString(args, 2),
                ConditionExpression = ParseString(args, 3)
            };
        }
    }
}
