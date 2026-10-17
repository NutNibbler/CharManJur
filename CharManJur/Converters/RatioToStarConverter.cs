using System.Globalization;

namespace CharManJur.Converters;

// Converts a 0-1 ratio into a GridLength star value, for building a proportional-fill
// bar out of two Grid columns. Pass ConverterParameter="invert" for the empty portion.
public class RatioToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double ratio = value is double d ? Math.Clamp(d, 0, 1) : 0;
        if (parameter as string == "invert") ratio = 1 - ratio;

        // GridLength can't be zero-width via Star(0) reliably in all renderers; floor it slightly.
        return new GridLength(Math.Max(ratio, 0.001), GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}