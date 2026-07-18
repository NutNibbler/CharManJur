using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return false;
        if (value is string str && string.IsNullOrWhiteSpace(str)) return false;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}