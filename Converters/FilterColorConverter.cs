using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class FilterColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
            return Colors.DeepSkyBlue;
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}