using System;

namespace AutoWizard.Core.Actions.Control
{
    public class SetVariableAction : AutoWizard.Core.Models.BaseAction
    {
        public string VariableName { get; set; } = string.Empty;
        public string ValueExpression { get; set; } = string.Empty;

        public override AutoWizard.Core.Models.ActionResult Execute(AutoWizard.Core.Models.ExecutionContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(VariableName))
                {
                    return new AutoWizard.Core.Models.ActionResult
                    {
                        Success = false,
                        Message = "Variable name is empty"
                    };
                }

                // 嘗試算術運算，否則視為字串
                string resolved = context.ResolveExpression(ValueExpression);
                object value;

                // Engine.ExpressionParser usage requires using AutoWizard.Core.Engine or full qualification
                if (AutoWizard.Core.Engine.ExpressionParser.TryEvaluateExpression(resolved, out double numResult))
                {
                    // 整數結果取整，否則保持浮點
                    value = numResult == Math.Floor(numResult) && !double.IsInfinity(numResult)
                        ? (object)(int)numResult
                        : numResult;
                }
                else if (bool.TryParse(resolved, out bool boolResult))
                {
                    value = boolResult;
                }
                else
                {
                    value = resolved;
                }

                context.Variables[VariableName] = value;
                context.Log($"Set {VariableName} = {value}");

                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = true,
                    Message = $"Variable '{VariableName}' set to '{value}'"
                };
            }
            catch (Exception ex)
            {
                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = false,
                    Message = $"SetVariable failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
