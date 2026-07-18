using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class DestroyModeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDestroyMode && isDestroyMode)
            return Colors.DarkRed;
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}