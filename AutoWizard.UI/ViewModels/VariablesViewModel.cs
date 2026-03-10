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
        public ObservableCollection<VariableDefinition> Variables { get; } = new();

        private VariableDefinition? _selectedVariable;
        public VariableDefinition? SelectedVariable
        {
            get => _selectedVariable;
            set
            {
                if (SetProperty(ref _selectedVariable, value))
                {
                    // 通知依賴於 SelectedVariable 的命令重新評估 CanExecute
                    DeleteVariableCommand.RaiseCanExecuteChanged();
                    DuplicateVariableCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Action delegate to be injected by MainWindowViewModel
        public Func<Task<(string Hex, int X, int Y)?>>? PickColorAction { get; set; }

        // Commands
        public DelegateCommand AddVariableCommand { get; }
        public DelegateCommand DeleteVariableCommand { get; }
        public DelegateCommand DuplicateVariableCommand { get; }
        public DelegateCommand<VariableDefinition> PickSpecificColorCommand { get; }

        public VariablesViewModel()
        {
            AddVariableCommand = new DelegateCommand(OnAddVariable);
            DeleteVariableCommand = new DelegateCommand(OnDeleteVariable, () => SelectedVariable != null);
            DuplicateVariableCommand = new DelegateCommand(OnDuplicateVariable, () => SelectedVariable != null);
            PickSpecificColorCommand = new DelegateCommand<VariableDefinition>(OnPickSpecificColor);
        }

        public void EnsureBuiltInColorVariables()
        {
            for (int i = 1; i <= 10; i++)
            {
                string name = $"color_check_{i}";
                var existing = Variables.FirstOrDefault(v => v.Name == name);
                if (existing == null)
                {
                    Variables.Add(new VariableDefinition
                    {
                        Name = name,
                        Type = VariableType.String,
                        DefaultValue = "0,0,#FFFFFF,0",
                        Description = $"內置色彩動態檢查變數 {i}"
                    });
                }
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

        private void OnAddVariable()
        {
            // 產生唯一名稱
            int count = Variables.Count + 1;
            string name;
            do
            {
                name = $"var{count}";
                count++;
            } while (Variables.Any(v => v.Name == name));

            var variable = new VariableDefinition
            {
                Name = name,
                Type = VariableType.String,
                DefaultValue = "",
                Description = ""
            };

            Variables.Add(variable);
            SelectedVariable = variable;
        }

        private void OnDeleteVariable()
        {
            if (SelectedVariable == null) return;
            Variables.Remove(SelectedVariable);
            SelectedVariable = Variables.LastOrDefault();
        }

        private void OnDuplicateVariable()
        {
            if (SelectedVariable == null) return;

            // 產生唯一名稱
            string baseName = SelectedVariable.Name + "_copy";
            string name = baseName;
            int suffix = 1;
            while (Variables.Any(v => v.Name == name))
            {
                name = $"{baseName}{suffix}";
                suffix++;
            }

            var copy = new VariableDefinition
            {
                Name = name,
                Type = SelectedVariable.Type,
                DefaultValue = SelectedVariable.DefaultValue,
                Description = SelectedVariable.Description
            };

            Variables.Add(copy);
            SelectedVariable = copy;
        }

        /// <summary>
        /// 取得所有變數名稱（供屬性面板下拉選單使用）
        /// </summary>
        public string[] GetVariableNames()
        {
            return Variables.Select(v => v.Name).ToArray();
        }

        /// <summary>
        /// 將變數定義注入到 ExecutionContext
        /// </summary>
        public void PopulateContext(Core.Models.ExecutionContext context)
        {
            foreach (var v in Variables)
            {
                context.Variables[v.Name] = v.GetTypedDefaultValue() ?? "";
            }
        }
    }
}
