using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoWizard.UI.Converters
{
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDirty && isDirty)
            {
                return "未儲存 (Unsaved)";
            }
            return "已儲存 (Saved)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
