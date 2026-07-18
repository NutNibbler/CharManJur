using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class IsArmorCategoryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string category)
        {
            return category == "Armor";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}