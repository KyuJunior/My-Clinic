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
            if (parameter != null && parameter.ToString()?.ToLower() == "inverse")
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
