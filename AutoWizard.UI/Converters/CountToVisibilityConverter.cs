using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoWizard.UI.Converters
{
    /// <summary>
    /// 將數量轉換為可見性 (0 = Visible, >0 = Collapsed)
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
