using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoWizard.Core.Models
{
    public class ModuleParameter : INotifyPropertyChanged
    {
        public string OriginalActionId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private string _valueExpression = string.Empty;
        public string ValueExpression
        {
            get => _valueExpression;
            set
            {
                if (_valueExpression != value)
                {
                    _valueExpression = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
