using System.Globalization;
using Microsoft.Maui.Controls;

namespace CharManJur.Converters;

public class CompleteStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isComplete && isComplete)
            return "Creation Stage Complete";
        return "Creation Stage In Progress";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}