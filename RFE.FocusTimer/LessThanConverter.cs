using System;
using System.Globalization;
using System.Windows.Data;

namespace RFE.FocusTimer;

public class LessThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double currentValue && parameter != null)
        {
            if (double.TryParse(parameter.ToString(), out var threshold))
            {
                return currentValue < threshold;
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}