using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AutoWizard.Core.Models;
using AutoWizard.Core.Scripting; // For DslParser

namespace AutoWizard.Core.Actions.Control
{
    public class CallModuleAction : BaseAction
    {
        public string ModuleName { get; set; } = string.Empty;
        public string SourceFilePath { get; set; } = string.Empty;
        public ObservableCollection<ModuleParameter> Parameters { get; set; } = new();

        public override ActionResult Execute(AutoWizard.Core.Models.ExecutionContext context)
        {
            if (string.IsNullOrEmpty(SourceFilePath) || !File.Exists(SourceFilePath))
            {
                return new ActionResult { Success = false, Message = $"找不到模組檔案: {SourceFilePath}" };
            }

            try
            {
                // 1. 讀取並解析
                var package = AutoWizard.Core.Resources.AwsPackage.Load(SourceFilePath);
                var moduleActions = package.Actions;

                // 2. 應用參數覆寫
                var allActions = FlattenActions(moduleActions);
                foreach (var param in Parameters)
                {
                    var targetAction = allActions.FirstOrDefault(a => a.Id == param.OriginalActionId);
                    if (targetAction != null)
                    {
                        var prop = targetAction.GetType().GetProperty(param.PropertyName);
                        if (prop != null && prop.CanWrite)
                        {
                            // 解析使用者提供的值
                            string resolvedValue = context.ResolveExpression(param.ValueExpression);
                            
                            object? converted = ConvertValue(resolvedValue, prop.PropertyType);
                            if (converted != null)
                            {
                                prop.SetValue(targetAction, converted);
                            }
                        }
                    }
                }

                // 3. 執行
                foreach (var action in moduleActions)
                {
                    if (context.IsCancellationRequested)
                    {
                        return new ActionResult { Success = false, Message = "Execution cancelled." };
                    }

                    var result = action.ExecuteWithPolicy(context);
                    if (!result.Success && !action.ErrorPolicy.ContinueOnError)
                    {
                        return new ActionResult { Success = false, Message = $"模組執行失敗: {result.Message}" };
                    }
                }

                return new ActionResult { Success = true, Message = $"模組 {ModuleName} 執行完畢" };
            }
            catch (Exception ex)
            {
                return new ActionResult { Success = false, Message = $"模組 {ModuleName} 執行異常: {ex.Message}", Exception = ex };
            }
        }

        private List<BaseAction> FlattenActions(List<BaseAction> actions)
        {
            var result = new List<BaseAction>();
            foreach (var action in actions)
            {
                result.Add(action);
                if (action is ContainerAction container)
                {
                    result.AddRange(FlattenActions(container.Children));
                }
                else if (action is IfAction ifAction)
                {
                    result.AddRange(FlattenActions(ifAction.ThenActions.Cast<BaseAction>().ToList()));
                    result.AddRange(FlattenActions(ifAction.ElseActions.Cast<BaseAction>().ToList()));
                }
            }
            return result;
        }

        private object? ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int) && int.TryParse(value, out int i)) return i;
            if (targetType == typeof(double) && double.TryParse(value, out double d)) return d;
            if (targetType == typeof(bool) && bool.TryParse(value, out bool b)) return b;
            if (targetType.IsEnum && Enum.TryParse(targetType, value, true, out var e)) return e;
            return null;
        }
    }
}
