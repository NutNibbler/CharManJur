using System.Globalization;
using CharManJur.Models;

namespace CharManJur.Converters;

public class PackIdToNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return string.Empty;
        if (values[0] is not string packId) return string.Empty;
        if (values[1] is not IEnumerable<InstalledPackEntry> packs) return packId;

        var match = packs.FirstOrDefault(p => p.PackId == packId);
        return match?.Name ?? packId;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}