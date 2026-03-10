using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;

namespace AutoWizard.Core.Engine
{
    public class ScriptParser
    {
        public ScriptDefinition ParseScript(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                return JsonSerializer.Deserialize<ScriptDefinition>(json, options) 
                    ?? throw new Exception("Failed to deserialize script");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse script: {ex.Message}", ex);
            }
        }

        public List<BaseAction> BuildActions(ScriptDefinition script)
        {
            var actions = new List<BaseAction>();
            foreach (var actionDef in script.Workflow)
            {
                var action = BuildAction(actionDef);
                if (action != null) actions.Add(action);
            }
            return actions;
        }

        private BaseAction? BuildAction(ActionDefinition def)
        {
            BaseAction? action = def.Type switch
            {
                "Click" => BuildClickAction(def),
                "Type" => BuildTypeAction(def),
                "If" => BuildIfAction(def),
                "Loop" => BuildLoopAction(def),
                "Wait" => BuildWaitAction(def),
                "Keyboard" => BuildKeyboardAction(def),
                "SetVariable" => BuildSetVariableAction(def),
                "Screenshot" => BuildScreenshotAction(def),
                _ => null
            };

            if (action != null)
            {
                action.Id = def.Id ?? Guid.NewGuid().ToString();
                action.Name = def.Name ?? def.Type;
                action.Description = def.Description ?? string.Empty;
                action.IsEnabled = def.IsEnabled ?? true;
                action.DelayBeforeMs = def.DelayBeforeMs ?? 0;
                action.DelayAfterMs = def.DelayAfterMs ?? 0;

                if (def.ErrorPolicy != null)
                {
                    action.ErrorPolicy = new ErrorHandlingPolicy
                    {
                        RetryCount = def.ErrorPolicy.RetryCount ?? 0,
                        RetryIntervalMs = def.ErrorPolicy.RetryIntervalMs ?? 1000,
                        JumpToLabel = def.ErrorPolicy.JumpToLabel,
                        ContinueOnError = def.ErrorPolicy.ContinueOnError ?? false
                    };
                }
            }
            return action;
        }

        private static int GetInt32Value(object value)
        {
            if (value is JsonElement je) return je.GetInt32();
            return Convert.ToInt32(value);
        }

        private static string GetStringValue(object value)
        {
            if (value is JsonElement je) return je.GetString() ?? string.Empty;
            return value?.ToString() ?? string.Empty;
        }

        private ClickAction? BuildClickAction(ActionDefinition def)
        {
            if (def.Parameters == null) return null;
            var action = new ClickAction();
            if (def.Parameters.TryGetValue("X", out var x)) action.X = GetInt32Value(x);
            if (def.Parameters.TryGetValue("Y", out var y)) action.Y = GetInt32Value(y);
            if (def.Parameters.TryGetValue("Button", out var button)) action.Button = Enum.Parse<MouseButton>(GetStringValue(button));
            if (def.Parameters.TryGetValue("ClickType", out var clickType)) action.ClickType = Enum.Parse<ClickType>(GetStringValue(clickType));
            if (def.Parameters.TryGetValue("XExpression", out var xe)) action.XExpression = GetStringValue(xe);
            if (def.Parameters.TryGetValue("YExpression", out var ye)) action.YExpression = GetStringValue(ye);
            return action;
        }

        private TypeAction? BuildTypeAction(ActionDefinition def)
        {
            if (def.Parameters == null) return null;
            var action = new TypeAction();
            if (def.Parameters.TryGetValue("Text", out var text)) action.Text = GetStringValue(text);
            if (def.Parameters.TryGetValue("Mode", out var mode)) action.Mode = Enum.Parse<InputMode>(GetStringValue(mode));
            if (def.Parameters.TryGetValue("IntervalMinMs", out var minMs)) action.IntervalMinMs = GetInt32Value(minMs);
            if (def.Parameters.TryGetValue("IntervalMaxMs", out var maxMs)) action.IntervalMaxMs = GetInt32Value(maxMs);
            return action;
        }

        private IfAction? BuildIfAction(ActionDefinition def)
        {
            if (def.Parameters == null) return null;
            var action = new IfAction();
            if (def.Parameters.TryGetValue("ConditionType", out var condType)) action.ConditionType = Enum.Parse<ConditionType>(GetStringValue(condType));
            if (def.Parameters.TryGetValue("LeftOperand", out var left)) action.LeftOperand = GetStringValue(left);
            if (def.Parameters.TryGetValue("RightOperand", out var right)) action.RightOperand = GetStringValue(right);
            if (def.Parameters.TryGetValue("ConditionExpression", out var ce)) action.ConditionExpression = GetStringValue(ce);

            if (def.ThenActions != null)
            {
                foreach (var childDef in def.ThenActions)
                {
                    var child = BuildAction(childDef);
                    if (child != null) action.ThenActions.Add(child);
                }
            }

            if (def.ElseActions != null)
            {
                foreach (var childDef in def.ElseActions)
                {
                    var child = BuildAction(childDef);
                    if (child != null) action.ElseActions.Add(child);
                }
            }
            return action;
        }

        private LoopAction? BuildLoopAction(ActionDefinition def)
        {
            if (def.Parameters == null) return null;
            var action = new LoopAction();
            if (def.Parameters.TryGetValue("LoopType", out var lt)) action.LoopType = Enum.Parse<LoopType>(GetStringValue(lt));
            if (def.Parameters.TryGetValue("Count", out var count)) action.Count = GetInt32Value(count);
            if (def.Parameters.TryGetValue("WhileCondition", out var wc)) action.WhileCondition = GetStringValue(wc);
            if (def.Parameters.TryGetValue("ForeachVariable", out var fv)) action.ForeachVariable = GetStringValue(fv);
            // ForeachList handling requires more logic if passed as JSON array, skipping deep parsing for now or adding basic list support? 
            // The original code had Collection parameter, but LoopAction now has ForeachList. 
            // Let's assume ScriptDefinition might pass it as List or JsonElement.
            // For now, sticking to what was likely there or minimal.
            // checking view_file of LoopAction: public List<string> ForeachList { get; set; }
            return action;
        }

        private WaitAction? BuildWaitAction(ActionDefinition def)
        {
            var action = new WaitAction();
            if (def.Parameters != null)
            {
                if (def.Parameters.TryGetValue("DurationMs", out var dur)) action.DurationMs = GetInt32Value(dur);
                if (def.Parameters.TryGetValue("DurationExpression", out var durExpr)) action.DurationExpression = GetStringValue(durExpr);
                if (def.Parameters.TryGetValue("WaitType", out var wt)) action.WaitType = Enum.Parse<WaitType>(GetStringValue(wt));
                if (def.Parameters.TryGetValue("RandomMinMs", out var rmin)) action.RandomMinMs = GetInt32Value(rmin);
                if (def.Parameters.TryGetValue("RandomMaxMs", out var rmax)) action.RandomMaxMs = GetInt32Value(rmax);
            }
            return action;
        }

        private SetVariableAction? BuildSetVariableAction(ActionDefinition def)
        {
            var action = new SetVariableAction();
            if (def.Parameters != null)
            {
                if (def.Parameters.TryGetValue("VariableName", out var vn)) action.VariableName = GetStringValue(vn);
                if (def.Parameters.TryGetValue("ValueExpression", out var ve)) action.ValueExpression = GetStringValue(ve);
            }
            return action;
        }

        private KeyboardAction? BuildKeyboardAction(ActionDefinition def)
        {
            var action = new KeyboardAction();
            if (def.Parameters != null)
            {
                if (def.Parameters.TryGetValue("Key", out var key)) action.Key = GetStringValue(key);
                if (def.Parameters.TryGetValue("Modifiers", out var mods)) action.Modifiers = Enum.Parse<KeyModifiers>(GetStringValue(mods));
                if (def.Parameters.TryGetValue("HoldDurationMs", out var hold)) action.HoldDurationMs = GetInt32Value(hold);
            }
            return action;
        }

        private ScreenshotAction? BuildScreenshotAction(ActionDefinition def)
        {
            var action = new ScreenshotAction();
            if (def.Parameters != null)
            {
                if (def.Parameters.TryGetValue("SavePath", out var sp)) action.SavePath = GetStringValue(sp);
                if (def.Parameters.TryGetValue("SaveToVariable", out var sv)) action.SaveToVariable = GetStringValue(sv);
                if (def.Parameters.TryGetValue("RegionX", out var rx)) action.RegionX = GetInt32Value(rx);
                if (def.Parameters.TryGetValue("RegionY", out var ry)) action.RegionY = GetInt32Value(ry);
                if (def.Parameters.TryGetValue("RegionWidth", out var rw)) action.RegionWidth = GetInt32Value(rw);
                if (def.Parameters.TryGetValue("RegionHeight", out var rh)) action.RegionHeight = GetInt32Value(rh);
            }
            return action;
        }
    }

    public class ScriptDefinition
    {
        public ScriptMetadata? Metadata { get; set; }
        public List<ScriptVariableDefinition> Variables { get; set; } = new();
        public List<ActionDefinition> Workflow { get; set; } = new();
    }

    public class ScriptMetadata
    {
        public string Version { get; set; } = "1.0";
        public string? Author { get; set; }
        public int CreatedDPI { get; set; } = 96;
        public string? Resolution { get; set; }
    }

    public class ScriptVariableDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = "String";
    }

    public class ActionDefinition
    {
        public string? Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsEnabled { get; set; }
        public int? DelayBeforeMs { get; set; }
        public int? DelayAfterMs { get; set; }
        public ErrorPolicyDefinition? ErrorPolicy { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<ActionDefinition>? Children { get; set; }
        public List<ActionDefinition>? ThenActions { get; set; }
        public List<ActionDefinition>? ElseActions { get; set; }
    }

    public class ErrorPolicyDefinition
    {
        public int? RetryCount { get; set; }
        public int? RetryIntervalMs { get; set; }
        public string? JumpToLabel { get; set; }
        public bool? ContinueOnError { get; set; }
    }
}
