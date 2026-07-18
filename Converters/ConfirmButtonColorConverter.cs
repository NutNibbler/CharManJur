using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class ConfirmButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasSelected && hasSelected)
            return Colors.DeepSkyBlue;
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}