using System.Globalization;

namespace CharManJur.Converters;

public class LoadedButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isLoaded && isLoaded ? "Unload" : "Load";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}