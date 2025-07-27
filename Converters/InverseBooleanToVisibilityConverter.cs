using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TradeDataEXP.Converters;

/// <summary>
/// Converts a boolean value to Visibility, with inverse logic
/// True -> Hidden/Collapsed, False -> Visible
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Inverse logic: true becomes hidden, false becomes visible
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            // Inverse logic: visible becomes false, collapsed/hidden becomes true
            return visibility != Visibility.Visible;
        }
        return false;
    }
}
