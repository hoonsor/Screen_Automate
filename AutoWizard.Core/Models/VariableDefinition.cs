using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AutoWizard.Core.Models
{
    /// <summary>
    /// 變數類型
    /// </summary>
    public enum VariableType
    {
        String,
        Integer,
        Double,
        Boolean
    }

    /// <summary>
    /// 設計時期的變數定義
    /// </summary>
    public class VariableDefinition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _name = string.Empty;
        /// <summary>變數名稱（唯一識別）</summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    OnPropertyChanged(nameof(IsBuiltInColor));
                }
            }
        }

        private VariableType _type = VariableType.String;
        /// <summary>變數類型</summary>
        public VariableType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string _defaultValue = string.Empty;
        /// <summary>預設值（文字表示）</summary>
        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        private string _description = string.Empty;
        /// <summary>說明</summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>指示是否為預設建立的內置顏色變數 (color_1 到 color_10)</summary>
        [JsonIgnore]
        public bool IsBuiltInColor
        {
            get
            {
                if (string.IsNullOrEmpty(Name) || !Name.StartsWith("color_")) return false;
                var numStr = Name.Substring(6);
                return int.TryParse(numStr, out int num) && num >= 1 && num <= 10;
            }
        }

        /// <summary>
        /// 將預設值轉換為正確的 CLR 類型
        /// </summary>
        public object? GetTypedDefaultValue()
        {
            return Type switch
            {
                VariableType.String => DefaultValue,
                VariableType.Integer => int.TryParse(DefaultValue, out var i) ? i : 0,
                VariableType.Double => double.TryParse(DefaultValue, out var d) ? d : 0.0,
                VariableType.Boolean => bool.TryParse(DefaultValue, out var b) && b,
                _ => DefaultValue
            };
        }
    }
}
