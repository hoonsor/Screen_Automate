using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.ViewModels
{
    public class PropertiesViewModel : BindableBase
    {
        // 排除清單：不在面板顯示的內部屬性
        private static readonly HashSet<string> ExcludedProperties = new()
        {
            "ErrorPolicy", "Children", "ThenActions", "ElseActions",
            "Id", "Description"
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

            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (ExcludedProperties.Contains(prop.Name)) continue;
                
                // 排除複雜物件屬性（集合、自訂類別等）
                if (prop.PropertyType.IsClass 
                    && prop.PropertyType != typeof(string)
                    && !prop.PropertyType.IsEnum)
                    continue;

                // IfAction 條件屬性動態過濾
                if (SelectedItem is AutoWizard.Core.Actions.Control.IfAction ifAction)
                {
                    if (ifAction.ConditionType == AutoWizard.Core.Actions.Control.ConditionType.ColorMatch)
                    {
                        if (prop.Name is "LeftOperand" or "RightOperand" or "ConditionExpression") 
                            continue;
                    }
                    else if (ifAction.ConditionType == AutoWizard.Core.Actions.Control.ConditionType.Expression)
                    {
                        if (prop.Name is "LeftOperand" or "RightOperand" or "ColorXExpression" or "ColorYExpression" or "TargetColor" or "Tolerance") 
                            continue;
                    }
                    else // 其他比較類型
                    {
                        if (prop.Name is "ConditionExpression" or "ColorXExpression" or "ColorYExpression" or "TargetColor" or "Tolerance") 
                            continue;
                    }
                }

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

            // Manually add ErrorPolicy properties
            if (SelectedItem is BaseAction action && action.ErrorPolicy != null)
            {
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
        public System.Type? PropertyType { get; set; } // 新增：屬性類型
        public bool IsEnum { get; set; }
        public IEnumerable<string>? EnumValues { get; set; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value) && Target != null)
                {
                    // 回寫至目標物件
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

                    // Special Sync: If user changes "Name", also update "Description" to keep DSL comment in sync
                    if (PropertyName == "Name" && converted is string newName)
                    {
                        var descProp = Target.GetType().GetProperty("Description");
                        if (descProp != null && descProp.CanWrite)
                        {
                            descProp.SetValue(Target, newName);
                        }
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
