using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MedicalApp.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasValue = value != null;
            if (value is string str)
            {
                hasValue = !string.IsNullOrWhiteSpace(str);
            }
            if (parameter != null && (parameter.ToString()?.ToLower() == "inverse" || parameter.ToString()?.ToLower() == "invert"))
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
