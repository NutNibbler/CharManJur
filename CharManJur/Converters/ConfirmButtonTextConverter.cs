using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class ConfirmButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasSelected && hasSelected)
            return "✅ Confirm Background";
        return "Select a Background";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}