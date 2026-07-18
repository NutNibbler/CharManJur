using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class StatButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string selectedStat && parameter is string statName)
        {
            return selectedStat == statName ? Colors.DeepSkyBlue : Colors.Gray;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}