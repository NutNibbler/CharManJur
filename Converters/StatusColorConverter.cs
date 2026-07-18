using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isComplete && isComplete)
            return Colors.LightGreen;
        return Colors.Orange;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}