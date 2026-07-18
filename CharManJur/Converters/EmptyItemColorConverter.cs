using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class EmptyItemColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEmpty && isEmpty)
            return Color.FromArgb("#2d1a1a"); // Dark red tint for empty items
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}