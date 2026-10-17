using System.Globalization;

namespace CharManJur.Converters;

public class EnabledButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEnabled = value is bool b && b;
        return isEnabled
            ? ThemeColorLookup.Get("ThemePrimaryButtonColor", Color.FromArgb("#3498db"))
            : Color.FromArgb("#808080");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}