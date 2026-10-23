using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Vision;

namespace AutoWizard.UI.Views
{
    public abstract class ImportTreeItem : Prism.Mvvm.BindableBase { }

    public class ActionTreeItem : ImportTreeItem
    {
        public string ActionName { get; set; } = string.Empty;
        public ObservableCollection<ImportTreeItem> Children { get; set; } = new();
    }

    public class PropertyTreeItem : ImportTreeItem
    {
        public string OriginalActionId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public partial class ModuleImportWindow : Window
    {
        public ObservableCollection<ImportTreeItem> RootItems { get; set; } = new();
        public List<ModuleParameter> SelectedParameters { get; private set; } = new();
        public string ApprovedModuleName { get; private set; } = string.Empty;

        // 排除跟 PropertiesViewModel 一樣的系統內部屬性
        private static readonly HashSet<string> ExcludedProperties = new()
        {
            "ErrorPolicy", "Children", "ThenActions", "ElseActions",
            "Id", "Description", "Conditions", "ConditionRelation"
            // "ConditionType", "LeftOperand", "RightOperand", 
            // "ConditionExpression", "ColorXExpression", "ColorYExpression",
            // "TargetColor", "Tolerance" 被移除排除，允許在插入參數時勾選
        };

        public ModuleImportWindow(string fileName, List<BaseAction> parsedActions)
        {
            InitializeComponent();
            
            string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            // 預設將非法的字元替換掉，中文則保留讓使用者決定是否要換
            TxtModuleName.Text = Regex.Replace(baseName, @"[^\w\u4e00-\u9fa5]", "_");

            BuildTree(parsedActions, RootItems);
            ActionsTreeView.ItemsSource = RootItems;
        }

        private void BuildTree(IEnumerable<BaseAction> actions, ObservableCollection<ImportTreeItem> targetCollection)
        {
            foreach (var action in actions)
            {
                var actionItem = new ActionTreeItem
                {
                    ActionName = GetFriendlyActionName(action)
                };

                // 反射取得所有可變更的屬性
                var type = action.GetType();
                foreach (var prop in type.GetProperties())
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    if (ExcludedProperties.Contains(prop.Name)) continue;

                    // 僅列出字串、數值、布林與列舉
                    if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsEnum) continue;

                    var value = prop.GetValue(action);
                    
                    var propItem = new PropertyTreeItem
                    {
                        OriginalActionId = action.Id,
                        PropertyName = prop.Name,
                        DisplayName = GetFriendlyPropertyName(prop.Name),
                        OriginalValue = value?.ToString() ?? string.Empty
                    };
                    actionItem.Children.Add(propItem);
                }

                // IfAction 或 ContainerAction 子節點遞迴
                if (action is IfAction ifAction)
                {
                    if (ifAction.ThenActions.Any())
                    {
                        var thenItem = new ActionTreeItem { ActionName = "條件成立 (Then)" };
                        BuildTree(ifAction.ThenActions.Cast<BaseAction>(), thenItem.Children);
                        actionItem.Children.Add(thenItem);
                    }
                    if (ifAction.ElseActions.Any())
                    {
                        var elseItem = new ActionTreeItem { ActionName = "條件不成立 (Else)" };
                        BuildTree(ifAction.ElseActions.Cast<BaseAction>(), elseItem.Children);
                        actionItem.Children.Add(elseItem);
                    }
                }
                else if (action is ContainerAction container)
                {
                    BuildTree(container.Children, actionItem.Children);
                }

                targetCollection.Add(actionItem);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string moduleName = TxtModuleName.Text.Trim();
            if (string.IsNullOrEmpty(moduleName))
            {
                MessageBox.Show("請輸入模組函數名稱！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(moduleName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                MessageBox.Show("模組名稱必須符合一般的英文變數命名規則 (英文字母或底線開頭，只能包含英文字母、數字和底線)。不能包含中文或特殊符號。", "命名無效", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApprovedModuleName = moduleName;

            // 收集勾選的參數
            SelectedParameters.Clear();
            CollectSelectedParameters(RootItems, actionNamePrefix: "");

            DialogResult = true;
            Close();
        }

        private void CollectSelectedParameters(IEnumerable<ImportTreeItem> items, string actionNamePrefix)
        {
            int paramIndex = SelectedParameters.Count + 1;
            foreach (var item in items)
            {
                if (item is PropertyTreeItem propItem && propItem.IsSelected)
                {
                    SelectedParameters.Add(new ModuleParameter
                    {
                        OriginalActionId = propItem.OriginalActionId,
                        PropertyName = propItem.PropertyName,
                        ParameterName = $"參數{paramIndex++}",
                        Description = string.IsNullOrEmpty(actionNamePrefix) 
                            ? propItem.DisplayName 
                            : $"{actionNamePrefix} 的 {propItem.DisplayName}",
                        ValueExpression = propItem.OriginalValue
                    });
                }
                else if (item is ActionTreeItem actionItem)
                {
                    // Update paramIndex inside recursive call to ensure correctness
                    CollectSelectedParametersRecursive(actionItem.Children, actionItem.ActionName, ref paramIndex);
                }
            }
        }

        private void CollectSelectedParametersRecursive(IEnumerable<ImportTreeItem> items, string actionNamePrefix, ref int paramIndex)
        {
            foreach (var item in items)
            {
                if (item is PropertyTreeItem propItem && propItem.IsSelected)
                {
                    SelectedParameters.Add(new ModuleParameter
                    {
                        OriginalActionId = propItem.OriginalActionId,
                        PropertyName = propItem.PropertyName,
                        ParameterName = $"參數{paramIndex++}",
                        Description = $"{actionNamePrefix} 的 {propItem.DisplayName}",
                        ValueExpression = propItem.OriginalValue
                    });
                }
                else if (item is ActionTreeItem actionItem)
                {
                    CollectSelectedParametersRecursive(actionItem.Children, actionItem.ActionName, ref paramIndex);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string GetFriendlyActionName(BaseAction action)
        {
            string name = action switch
            {
                ClickAction => "點擊",
                TypeAction => "輸入文字",
                IfAction => "條件判斷",
                LoopAction => "迴圈",
                FindImageAction => "尋找影像",
                OCRAction => "OCR 辨識",
                WaitAction => "等待",
                KeyboardAction => "快捷鍵",
                SetVariableAction => "設定變數",
                ScreenshotAction => "螢幕截圖",
                _ => action.GetType().Name
            };
            var desc = action.Description;
            return string.IsNullOrEmpty(desc) ? name : $"{name} ({desc})";
        }

        private static string GetFriendlyPropertyName(string name)
        {
            return name switch
            {
                "Name" => "名稱", "IsEnabled" => "啟用", "DelayBeforeMs" => "前置延遲(ms)", "DelayAfterMs" => "後置延遲(ms)",
                "X" => "X 座標", "Y" => "Y 座標", "Button" => "按鈕", "ClickType" => "點擊類型",
                "Text" => "文字內容", "Mode" => "輸入模式", "IntervalMinMs" => "最小間隔(ms)", "IntervalMaxMs" => "最大間隔(ms)",
                "LoopType" => "迴圈類型", "Count" => "次數", "ForeachVariable" => "迭代變數", "WhileCondition" => "While 條件",
                "TemplateImagePath" => "範本圖片路徑", "Threshold" => "相似度門檻", "TimeoutMs" => "逾時(ms)", "IntervalMs" => "間隔(ms)",
                "ClickWhenFound" => "找到後點擊", "SaveToVariable" => "儲存至變數", "Language" => "語言",
                "SearchText" => "搜尋文字", "UseRegex" => "正規表達式", "DurationMs" => "延遲(ms)", "DurationExpression" => "延遲表達式",
                "WaitType" => "等待類型", "RandomMinMs" => "隨機最小(ms)", "RandomMaxMs" => "隨機最大(ms)",
                "Key" => "按鍵", "Modifiers" => "修飾鍵", "HoldDurationMs" => "按住時長(ms)",
                "VariableName" => "變數名稱", "ValueExpression" => "值表達式", "SavePath" => "儲存路徑",
                "RegionX" => "區域 X", "RegionY" => "區域 Y", "RegionWidth" => "區域寬度", "RegionHeight" => "區域高度",
                "XExpression" => "X 表達式", "YExpression" => "Y 表達式", "VariableType" => "類型",
                _ => name
            };
        }
    }
}
