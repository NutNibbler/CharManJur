using System.Globalization;
using CharManJur.Models;

namespace CharManJur.Converters;

public class IsPairedTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LimbPairType pairType)
        {
            return pairType == LimbPairType.Paired;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}