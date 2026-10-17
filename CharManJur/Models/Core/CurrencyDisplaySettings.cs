namespace CharManJur.Models;

public enum CurrencyDisplayUnit
{
    Chips,
    Rupees,
    Marks
}

// How currency totals are displayed (dashboard now, shop later). App-wide, saved on this device.
// Only affects display; stored values are always chips.
public static class CurrencyDisplaySettings
{
    private const string PreferenceKey = "CurrencyDisplayUnit";

    public static event Action? Changed;

    public static CurrencyDisplayUnit Unit
    {
        get
        {
            string saved = Preferences.Default.Get(PreferenceKey, nameof(CurrencyDisplayUnit.Chips));
            return Enum.TryParse(saved, out CurrencyDisplayUnit unit) && Enum.IsDefined(unit)
                ? unit
                : CurrencyDisplayUnit.Chips;
        }
        set
        {
            if (value == Unit) return;
            Preferences.Default.Set(PreferenceKey, value.ToString());
            Changed?.Invoke();
        }
    }
}