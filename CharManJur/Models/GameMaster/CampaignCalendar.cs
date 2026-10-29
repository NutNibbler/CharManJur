using System.Text.Json.Serialization;

namespace CharManJur.Models;

// One resolved calendar date (what the dashboard headline shows).
public sealed record CalendarDate(
    int Day, int Year, bool ShowYear,
    int MonthIndex, string MonthName,
    int DayOfMonth, int WeekOfMonth,
    int DayOfWeekIndex, string DayName)
{
    public string Long => ShowYear
        ? $"{DayName}, {MonthName} {DayOfMonth}, {Year}"
        : $"{DayName}, {MonthName} {DayOfMonth}";

    public string Header => $"Day {Day} · {Long}";
}

// The calendar a campaign runs on, set by the GM when the campaign is created.
// Defaults come from the rulebook's Tiverian Calendar: 12 months of 4 weeks, 7-day weeks (336 days a year).
// The rulebook gives no hours-per-day figure, so 24 is used.
public class CalendarConfig
{
    public static readonly string[] RulebookMonths =
    {
        "Ceprii", "Akarii", "Pisce", "Arii", "Taurec", "Gannii",
        "Canna", "Lea", "Virii", "Libara", "Skarii", "Sagarii"
    };

    public static readonly string[] RulebookDays =
        { "Mona", "Tulas", "Wora", "Teres", "Fira", "Seres", "Sona" };

    public List<string> MonthNames { get; set; } = new(RulebookMonths);
    public List<string> DayNames { get; set; } = new(RulebookDays);
    public int WeeksPerMonth { get; set; } = 4;
    public int HoursPerDay { get; set; } = 24;
    // Which day of the week day 1 falls on: an index into DayNames (0 = the first day name).
    public int StartDayOfWeek { get; set; }

    // The setting is the Age of Fallen Stars, 443 years since Kithiir died. Optional.
    public bool ShowYear { get; set; } = true;
    public int StartYear { get; set; } = 443;

    [JsonIgnore] public int MonthsPerYear => MonthNames.Count;
    [JsonIgnore] public int DaysPerWeek => DayNames.Count;
    [JsonIgnore] public int DaysPerMonth => DaysPerWeek * WeeksPerMonth;
    [JsonIgnore] public int DaysPerYear => DaysPerMonth * MonthsPerYear;

    // Changing a count in the creator keeps the names that exist and fills new slots with "Month 13" etc.
    public void SetMonthCount(int count) => Resize(MonthNames, Math.Clamp(count, 1, 24), "Month");
    public void SetDaysPerWeek(int count)
    {
        Resize(DayNames, Math.Clamp(count, 1, 14), "Day");
        StartDayOfWeek = Math.Clamp(StartDayOfWeek, 0, DayNames.Count - 1);
    }

    private static void Resize(List<string> names, int count, string prefix)
    {
        while (names.Count > count) names.RemoveAt(names.Count - 1);
        while (names.Count < count) names.Add($"{prefix} {names.Count + 1}");
    }

    // Day 1 is the first day name of the first month of the starting year. Days count up forever.
    public CalendarDate DateFor(int day)
    {
        day = Math.Max(1, day);
        int perWeek = Math.Max(1, DaysPerWeek);
        int perMonth = Math.Max(1, DaysPerMonth);
        int perYear = Math.Max(1, DaysPerYear);

        int zero = day - 1;
        int yearsPassed = zero / perYear;
        int dayOfYear = zero % perYear;
        int month = dayOfYear / perMonth;
        int dayOfMonth = dayOfYear % perMonth;
        // Months are whole weeks, so weekdays run on unbroken; the starting day shifts where day 1 lands.
        int start = Math.Clamp(StartDayOfWeek, 0, perWeek - 1);
        int weekday = (start + zero) % perWeek;

        return new CalendarDate(
            Day: day,
            Year: StartYear + yearsPassed,
            ShowYear: ShowYear,
            MonthIndex: month,
            MonthName: month < MonthNames.Count ? MonthNames[month] : $"Month {month + 1}",
            DayOfMonth: dayOfMonth + 1,
            WeekOfMonth: dayOfMonth / perWeek + 1,
            DayOfWeekIndex: weekday,
            DayName: weekday < DayNames.Count ? DayNames[weekday] : $"Day {weekday + 1}");
    }

    // A 24-hour day reads as a clock; any other length reads as "Hour 3 of 20".
    public string HourLabel(int hour)
    {
        hour = Math.Clamp(hour, 0, Math.Max(1, HoursPerDay) - 1);
        return HoursPerDay == 24 ? $"{hour:00}:00" : $"Hour {hour + 1} of {HoursPerDay}";
    }

    public string? Validate()
    {
        if (MonthNames.Count is < 1 or > 24) return "A year needs 1 to 24 months.";
        if (DayNames.Count is < 1 or > 14) return "A week needs 1 to 14 days.";
        if (WeeksPerMonth is < 1 or > 8) return "A month needs 1 to 8 weeks.";
        if (HoursPerDay is < 1 or > 48) return "A day needs 1 to 48 hours.";

        if (StartDayOfWeek < 0 || StartDayOfWeek >= DayNames.Count) return "Pick a starting day of the week.";

        if (MonthNames.Any(string.IsNullOrWhiteSpace)) return "Every month needs a name.";
        if (DayNames.Any(string.IsNullOrWhiteSpace)) return "Every day of the week needs a name.";

        if (MonthNames.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != MonthNames.Count)
            return "Two months share a name.";
        if (DayNames.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != DayNames.Count)
            return "Two days of the week share a name.";

        return null;
    }
}