using System.Globalization;

namespace CharManJur.Converters;

public class IsCustomBackgroundConverter : IValueConverter
{
    private const int CustomIdStart = 90001;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int id)
        {
            return id >= CustomIdStart;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}