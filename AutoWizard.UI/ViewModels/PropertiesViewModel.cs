using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Control;

namespace AutoWizard.UI.ViewModels
{
    public class PropertiesViewModel : BindableBase
    {
        // 排除清單：不在面板顯示的內部屬性
        private static readonly HashSet<string> ExcludedProperties = new()
        {
            "ErrorPolicy", "Children", "ThenActions", "ElseActions",
            "Id", "Description", "Conditions", "ConditionRelation",
            // 向下相容代理屬性（由多條件機制取代）
            "ConditionType", "LeftOperand", "RightOperand",
            "ConditionExpression", "ColorXExpression", "ColorYExpression",
            "TargetColor", "Tolerance"
        };

        private object? _selectedItem;
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    UpdateProperties();
                }
            }
        }

        public ObservableCollection<PropertyEntry> Properties { get; } = new();

        private string _itemType = "未選擇";
        public string ItemType
        {
            get => _itemType;
            set => SetProperty(ref _itemType, value);
        }

        private void UpdateProperties()
        {
            Properties.Clear();

            if (SelectedItem == null)
            {
                ItemType = "未選擇";
                return;
            }

            var type = SelectedItem.GetType();
            ItemType = GetFriendlyTypeName(type.Name);

            // 通用屬性
            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (ExcludedProperties.Contains(prop.Name)) continue;
                
                if (prop.PropertyType.IsClass 
                    && prop.PropertyType != typeof(string)
                    && !prop.PropertyType.IsEnum)
                    continue;

                var value = prop.GetValue(SelectedItem);
                string displayValue = value?.ToString() ?? string.Empty;

                bool isEnum = prop.PropertyType.IsEnum;
                IEnumerable<string>? enumValues = isEnum ? System.Enum.GetNames(prop.PropertyType) : null;

                Properties.Add(new PropertyEntry
                {
                    Key = GetFriendlyPropertyName(prop.Name),
                    Value = displayValue,
                    PropertyName = prop.Name,
                    Target = SelectedItem,
                    PropertyType = prop.PropertyType,
                    IsEnum = isEnum,
                    EnumValues = enumValues
                });
            }

            // IfAction 多條件渲染
            if (SelectedItem is IfAction ifAction)
            {
                RenderIfConditions(ifAction);
            }

            // ErrorPolicy properties
            if (SelectedItem is BaseAction action && action.ErrorPolicy != null)
            {
                Properties.Add(new PropertyEntry
                {
                    Key = "重試次數",
                    Value = action.ErrorPolicy.RetryCount.ToString(),
                    PropertyName = "RetryCount",
                    Target = action.ErrorPolicy,
                    PropertyType = typeof(int)
                });
                Properties.Add(new PropertyEntry
                {
                    Key = "重試間隔(ms)",
                    Value = action.ErrorPolicy.RetryIntervalMs.ToString(),
                    PropertyName = "RetryIntervalMs",
                    Target = action.ErrorPolicy,
                    PropertyType = typeof(int)
                });
                Properties.Add(new PropertyEntry
                {
                    Key = "錯誤時繼續",
                    Value = action.ErrorPolicy.ContinueOnError.ToString(),
                    PropertyName = "ContinueOnError",
                    Target = action.ErrorPolicy,
                    PropertyType = typeof(bool)
                });
            }
        }

        /// <summary>
        /// 渲染 IfAction 的多條件區塊
        /// </summary>
        private void RenderIfConditions(IfAction ifAction)
        {
            // 新增/移除條件按鈕
            Properties.Add(new PropertyEntry
            {
                Key = "➕ 新增條件",
                IsButton = true,
                ButtonAction = () =>
                {
                    ifAction.Conditions.Add(new ConditionItem());
                    // 自動更新 ConditionRelation
                    ifAction.ConditionRelation = BuildDefaultRelation(ifAction.Conditions.Count);
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                        new System.Action(() => UpdateProperties()));
                }
            });

            if (ifAction.Conditions.Count > 1)
            {
                Properties.Add(new PropertyEntry
                {
                    Key = "➖ 移除最後條件",
                    IsButton = true,
                    ButtonAction = () =>
                    {
                        if (ifAction.Conditions.Count > 1)
                        {
                            ifAction.Conditions.RemoveAt(ifAction.Conditions.Count - 1);
                            ifAction.ConditionRelation = BuildDefaultRelation(ifAction.Conditions.Count);
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                                new System.Action(() => UpdateProperties()));
                        }
                    }
                });
            }

            // 條件關係（僅多條件時顯示）
            if (ifAction.Conditions.Count > 1)
            {
                Properties.Add(new PropertyEntry
                {
                    Key = "條件關係",
                    Value = ifAction.ConditionRelation,
                    PropertyName = "ConditionRelation",
                    Target = ifAction,
                    PropertyType = typeof(string)
                });
            }

            // 逐條渲染每個條件
            for (int i = 0; i < ifAction.Conditions.Count; i++)
            {
                var cond = ifAction.Conditions[i];
                int condIndex = i; // capture

                // 分隔線
                Properties.Add(new PropertyEntry
                {
                    Key = $"━━ 條件 {i + 1} (C{i + 1}) ━━",
                    IsSeparator = true
                });

                // 條件類型
                Properties.Add(new PropertyEntry
                {
                    Key = "條件類型",
                    Value = cond.ConditionType.ToString(),
                    PropertyName = "ConditionType",
                    Target = cond,
                    PropertyType = typeof(ConditionType),
                    IsEnum = true,
                    EnumValues = System.Enum.GetNames(typeof(ConditionType)),
                    OnConditionTypeChanged = () =>
                        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                            new System.Action(() => UpdateProperties()))
                });

                // 根據條件類型顯示對應欄位
                RenderConditionFields(cond);
            }
        }

        /// <summary>
        /// 根據 ConditionItem 的 ConditionType 渲染對應的欄位
        /// </summary>
        private void RenderConditionFields(ConditionItem cond)
        {
            switch (cond.ConditionType)
            {
                case ConditionType.VariableEquals:
                case ConditionType.VariableNotEquals:
                case ConditionType.VariableGreaterThan:
                case ConditionType.VariableLessThan:
                    Properties.Add(CreateConditionEntry("變數/值 (左)", "LeftOperand", cond));
                    Properties.Add(CreateConditionEntry("比較值 (右)", "RightOperand", cond));
                    break;

                case ConditionType.VariableContains:
                    Properties.Add(CreateConditionEntry("搜尋目標字串", "LeftOperand", cond));
                    Properties.Add(CreateConditionEntry("包含的子字串", "RightOperand", cond));
                    break;

                case ConditionType.ImageExists:
                    Properties.Add(CreateConditionEntry("圖片路徑", "LeftOperand", cond));
                    Properties.Add(CreateConditionEntry("相似度門檻 (0~1)", "RightOperand", cond));
                    break;

                case ConditionType.FileExists:
                    Properties.Add(CreateConditionEntry("檔案路徑", "LeftOperand", cond));
                    break;

                case ConditionType.Expression:
                    Properties.Add(CreateConditionEntry("條件表達式", "ConditionExpression", cond));
                    break;

                case ConditionType.ColorMatch:
                    Properties.Add(CreateConditionEntry("目標 X (可用變數)", "ColorXExpression", cond));
                    Properties.Add(CreateConditionEntry("目標 Y (可用變數)", "ColorYExpression", cond));
                    Properties.Add(CreateConditionEntry("預期顏色 (#HEX 或 {var})", "TargetColor", cond));
                    Properties.Add(new PropertyEntry
                    {
                        Key = "顏色容差 (0-255)",
                        Value = cond.Tolerance.ToString(),
                        PropertyName = "Tolerance",
                        Target = cond,
                        PropertyType = typeof(int)
                    });
                    break;
            }
        }

        /// <summary>
        /// 建立條件欄位的 PropertyEntry（字串類型）
        /// </summary>
        private static PropertyEntry CreateConditionEntry(string label, string propertyName, ConditionItem cond)
        {
            var prop = typeof(ConditionItem).GetProperty(propertyName);
            string value = prop?.GetValue(cond)?.ToString() ?? string.Empty;
            return new PropertyEntry
            {
                Key = label,
                Value = value,
                PropertyName = propertyName,
                Target = cond,
                PropertyType = typeof(string)
            };
        }

        /// <summary>
        /// 建立預設條件關係表達式
        /// </summary>
        private static string BuildDefaultRelation(int count)
        {
            if (count <= 1) return "C1";
            var parts = Enumerable.Range(1, count).Select(i => $"C{i}");
            return string.Join(" && ", parts);
        }

        private static string GetFriendlyTypeName(string typeName)
        {
            return typeName switch
            {
                "ClickAction" => "🖱️ 點擊",
                "TypeAction" => "⌨️ 輸入文字",
                "IfAction" => "🔀 條件判斷",
                "LoopAction" => "🔁 迴圈",
                "FindImageAction" => "🔍 尋找影像",
                "OCRAction" => "📝 OCR 辨識",
                "WaitAction" => "⏳ 等待",
                "KeyboardAction" => "⌨️ 快捷鍵",
                "SetVariableAction" => "📌 設定變數",
                "ScreenshotAction" => "📷 螢幕截圖",
                _ => typeName
            };
        }

        private static string GetFriendlyPropertyName(string name)
        {
            return name switch
            {
                "Name" => "名稱",
                "IsEnabled" => "啟用",
                "DelayBeforeMs" => "前置延遲(ms)",
                "DelayAfterMs" => "後置延遲(ms)",
                "X" => "X 座標",
                "Y" => "Y 座標",
                "Button" => "按鈕",
                "ClickType" => "點擊類型",
                "Text" => "文字內容",
                "Mode" => "輸入模式",
                "IntervalMinMs" => "最小間隔(ms)",
                "IntervalMaxMs" => "最大間隔(ms)",
                "ConditionType" => "條件類型",
                "LeftOperand" => "左運算元",
                "RightOperand" => "右運算元",
                "LoopType" => "迴圈類型",
                "Count" => "次數",
                "ForeachVariable" => "迭代變數",
                "WhileCondition" => "While 條件",
                "TemplateImagePath" => "範本圖片路徑",
                "Threshold" => "相似度門檻",
                "TimeoutMs" => "逾時(ms)",
                "IntervalMs" => "間隔(ms)",
                "ClickWhenFound" => "找到後點擊",
                "SaveToVariable" => "儲存至變數",
                "Language" => "語言",
                "SearchText" => "搜尋文字",
                "UseRegex" => "正規表達式",
                "DurationMs" => "延遲(ms)",
                "DurationExpression" => "延遲表達式",
                "WaitType" => "等待類型",
                "RandomMinMs" => "隨機最小(ms)",
                "RandomMaxMs" => "隨機最大(ms)",
                "Key" => "按鍵",
                "Modifiers" => "修飾鍵",
                "HoldDurationMs" => "按住時長(ms)",
                "VariableName" => "變數名稱",
                "ValueExpression" => "值表達式",
                "SavePath" => "儲存路徑",
                "RegionX" => "區域 X",
                "RegionY" => "區域 Y",
                "RegionWidth" => "區域寬度",
                "RegionHeight" => "區域高度",
                "XExpression" => "X 表達式",
                "YExpression" => "Y 表達式",
                "ConditionExpression" => "條件表達式",
                "ColorXExpression" => "目標 X (可用變數)",
                "ColorYExpression" => "目標 Y (可用變數)",
                "TargetColor" => "預期顏色 (#HEX 或 {var})",
                "Tolerance" => "顏色容差 (0-255)",
                _ => name
            };
        }
    }

    /// <summary>
    /// 屬性面板的單項屬性
    /// </summary>
    public class PropertyEntry : BindableBase
    {
        public string Key { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public object? Target { get; set; }
        public System.Type? PropertyType { get; set; }
        public bool IsEnum { get; set; }
        public IEnumerable<string>? EnumValues { get; set; }

        /// <summary>是否為按鈕項目（新增/移除條件）</summary>
        public bool IsButton { get; set; }

        /// <summary>按鈕點擊時執行的動作</summary>
        public System.Action? ButtonAction { get; set; }

        /// <summary>ICommand 供 XAML Button 綁定</summary>
        public ICommand? ButtonCommand => ButtonAction != null ? new DelegateCommand(ButtonAction) : null;

        /// <summary>是否為分隔線項目（條件標題）</summary>
        public bool IsSeparator { get; set; }

        /// <summary>
        /// 當 ConditionType 值變更時觸發的回呼，用於重建屬性面板
        /// </summary>
        public System.Action? OnConditionTypeChanged { get; set; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value) && Target != null)
                {
                    TryWriteBack(value);
                }
            }
        }

        private void TryWriteBack(string newValue)
        {
            if (Target == null || string.IsNullOrEmpty(PropertyName)) return;

            try
            {
                var prop = Target.GetType().GetProperty(PropertyName);
                if (prop == null || !prop.CanWrite) return;

                var propType = prop.PropertyType;
                object? converted = null;

                if (propType == typeof(string))
                    converted = newValue;
                else if (propType == typeof(int))
                    converted = int.TryParse(newValue, out var i) ? i : null;
                else if (propType == typeof(double))
                    converted = double.TryParse(newValue, out var d) ? d : null;
                else if (propType == typeof(bool))
                    converted = bool.TryParse(newValue, out var b) ? b : null;
                else if (propType.IsEnum)
                    converted = System.Enum.TryParse(propType, newValue, true, out var e) ? e : null;

                if (converted != null)
                {
                    prop.SetValue(Target, converted);

                    // Special Sync: If user changes "Name", also update "Description"
                    if (PropertyName == "Name" && converted is string newName)
                    {
                        var descProp = Target.GetType().GetProperty("Description");
                        if (descProp != null && descProp.CanWrite)
                        {
                            descProp.SetValue(Target, newName);
                        }
                    }

                    // ConditionType 變更時，重建屬性面板
                    if (PropertyName == "ConditionType")
                    {
                        OnConditionTypeChanged?.Invoke();
                    }
                }
            }
            catch
            {
                // 無效的值，靜默忽略
            }
        }
    }
}
