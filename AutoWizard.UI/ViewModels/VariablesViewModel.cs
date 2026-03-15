using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.ViewModels
{
    public class VariablesViewModel : BindableBase
    {
        public ObservableCollection<VariableDefinition> BuiltInVariables { get; } = new();
        public ObservableCollection<VariableDefinition> CustomVariables { get; } = new();

        private VariableDefinition? _selectedBuiltInVariable;
        public VariableDefinition? SelectedBuiltInVariable
        {
            get => _selectedBuiltInVariable;
            set => SetProperty(ref _selectedBuiltInVariable, value);
        }

        private VariableDefinition? _selectedCustomVariable;
        public VariableDefinition? SelectedCustomVariable
        {
            get => _selectedCustomVariable;
            set => SetProperty(ref _selectedCustomVariable, value);
        }

        // Action delegate to be injected by MainWindowViewModel
        public Func<Task<(string Hex, int X, int Y)?>>? PickColorAction { get; set; }

        public DelegateCommand RefreshCustomVariablesCommand { get; }

        // Needs to be hooked up to MainWindowViewModel or EditorViewModel
        public Func<System.Collections.Generic.IEnumerable<AutoWizard.Core.Models.BaseAction>>? GetCurrentScriptActions { get; set; }

        public VariablesViewModel()
        {
            RefreshCustomVariablesCommand = new DelegateCommand(OnRefreshCustomVariables);
        }

        public void EnsureBuiltInVariables()
        {
            if (BuiltInVariables.Count == 0)
            {
                BuiltInVariables.Add(new VariableDefinition
                {
                    Name = "ColorCheck",
                    Type = VariableType.Boolean,
                    DefaultValue = "",
                    Description = "全域內置變數：判斷特定顏色是否符合條件"
                });

                BuiltInVariables.Add(new VariableDefinition
                {
                    Name = "FindPic",
                    Type = VariableType.Boolean,
                    DefaultValue = "",
                    Description = "全域內置變數：判斷特定圖片是否尋找到"
                });
            }
            
            if (SelectedBuiltInVariable == null) 
            {
                SelectedBuiltInVariable = BuiltInVariables.FirstOrDefault();
            }
        }

        private async void OnPickSpecificColor(VariableDefinition variable)
        {
            if (PickColorAction == null || variable == null) return;

            var result = await PickColorAction();
            if (result.HasValue)
            {
                // Format: X, Y, #HEX, Tolerance(default:0)
                variable.DefaultValue = $"{result.Value.X},{result.Value.Y},{result.Value.Hex},0";
            }
        }

        private void OnRefreshCustomVariables()
        {
            CustomVariables.Clear();

            if (GetCurrentScriptActions == null) return;

            var actions = GetCurrentScriptActions();
            var uniqueVarNames = new System.Collections.Generic.HashSet<string>();

            // Traverse recursively to find SetVariableAction
            void Traverse(System.Collections.Generic.IEnumerable<AutoWizard.Core.Models.BaseAction> list)
            {
                foreach (var action in list)
                {
                    if (action is AutoWizard.Core.Actions.Control.SetVariableAction setVar)
                    {
                        if (!string.IsNullOrWhiteSpace(setVar.VariableName) && !uniqueVarNames.Contains(setVar.VariableName))
                        {
                            uniqueVarNames.Add(setVar.VariableName);
                            CustomVariables.Add(new VariableDefinition
                            {
                                Name = setVar.VariableName,
                                Type = setVar.VariableType,
                                DefaultValue = setVar.ValueExpression, 
                                Description = "自訂變數 (取自腳本)"
                            });
                        }
                    }

                    if (action is AutoWizard.Core.Models.ContainerAction container)
                    {
                        if (container.Children != null) Traverse(container.Children);
                        
                        // Special handling for IfAction
                        if (action is AutoWizard.Core.Actions.Control.IfAction ifAction) 
                        {
                            if (ifAction.ThenActions != null) Traverse(ifAction.ThenActions);
                            if (ifAction.ElseActions != null) Traverse(ifAction.ElseActions);
                        }
                    }
                }
            }

            Traverse(actions);
            
            if (SelectedCustomVariable == null)
            {
                SelectedCustomVariable = CustomVariables.FirstOrDefault();
            }
        }

        /// <summary>
        /// 取得所有變數名稱（供屬性面板下拉選單使用）
        /// </summary>
        public string[] GetVariableNames()
        {
            var builtIn = BuiltInVariables.Select(v => v.Name);
            var custom = CustomVariables.Select(v => v.Name);
            return builtIn.Concat(custom).ToArray();
        }

        /// <summary>
        /// 將變數定義注入到 ExecutionContext
        /// </summary>
        public void PopulateContext(Core.Models.ExecutionContext context)
        {
            foreach (var v in BuiltInVariables)
            {
                context.Variables[v.Name] = v.GetTypedDefaultValue() ?? "";
            }
            foreach (var v in CustomVariables)
            {
                context.Variables[v.Name] = v.GetTypedDefaultValue() ?? "";
            }
        }
    }
}
