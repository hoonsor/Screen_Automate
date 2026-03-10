using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoWizard.UI.Converters
{
    public class ItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter is ItemsControl itemsControl)
            {
                var index = itemsControl.Items.IndexOf(value);
                if (index != -1)
                {
                    return (index + 1).ToString();
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
