using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class EquipmentStateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string state)
        {
            if (state.Contains("Armor")) return Colors.Green;
            if (state.Contains("Belted")) return Colors.Orange;
            if (state.Contains("Equipped")) return Colors.Cyan;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}