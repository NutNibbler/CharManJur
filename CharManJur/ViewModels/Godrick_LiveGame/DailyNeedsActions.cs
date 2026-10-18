using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// Dialogs for the daily-needs system: affirming needs, passing a day, explaining deprivation.
public static class DailyNeedsActions
{
    // Multiplayer seam: when a GM hosts a session, only the GM passes the day and ALWAYS gets the reminder.
    public static bool GmHosted => false;

    // The Daily Needs button. Returns true if anything changed.
    public static async Task<bool> AffirmNeedsAsync(ICharAttribDataService svc)
    {
        var page = Application.Current!.MainPage!;

        if (svc.NeedsMet)
        {
            await page.DisplayAlertAsync("Daily Needs", "Your needs are already met today.", "OK");
            return false;
        }

        bool ok = await page.DisplayAlertAsync("Daily Needs",
            "Confirm that your needs are met today: you've eaten, or done whatever upkeep your race requires (a Construct's oil, for example).",
            "Needs met", "Cancel");
        if (!ok) return false;

        bool wasDeprived = svc.IsDeprived;
        svc.AffirmNeedsMet();

        if (wasDeprived)
        {
            await page.DisplayAlertAsync("Needs Met",
                svc.IsDeprived
                    ? "Your needs are met. You're still Deprived until you also take a Long Rest."
                    : "Your needs are met and you're already rested, so you're no longer Deprived.",
                "OK");
        }
        return true;
    }

    // The Pass a Day button. A confirmation appears only when a chip is missing (always for a GM host).
    // `passDay` does the actual work (LiveFeatures.PassDayAsync). Returns true if the day passed.
    public static async Task<bool> PassDayAsync(ICharAttribDataService svc, Func<Task<FeatureUseResult>>? passDay)
    {
        if (passDay == null) return false;
        var page = Application.Current!.MainPage!;

        bool missingNeeds = !svc.NeedsMet;
        bool missingRest = !svc.Rested;

        if (missingNeeds || missingRest || GmHosted)
        {
            var parts = new List<string>();

            if (GmHosted)
                parts.Add("The day is about to end. Remind your players to handle their needs and rest first.");

            if (missingNeeds && missingRest)
                parts.Add("Your needs aren't met and you haven't taken a Long Rest today.");
            else if (missingNeeds)
                parts.Add("Your needs aren't met today.");
            else if (missingRest)
                parts.Add("You haven't taken a Long Rest today.");

            if (missingNeeds || missingRest)
            {
                parts.Add(svc.IsDeprived
                    ? "You're already Deprived, so this day costs you 1 Fatigue."
                    : "If the day ends now you become Deprived: no recovering Hit Protection or ability scores until you take a Long Rest and meet your needs.");
                parts.Add("Meet your needs or rest first, or pass the day anyway?");
            }

            bool ok = await page.DisplayAlertAsync("Pass a Day", string.Join("\n\n", parts), "Pass the day", "Cancel");
            if (!ok) return false;
        }

        var result = await passDay();
        await page.DisplayAlertAsync("A Day Passes", result.Message, "OK");
        return true;
    }

    // Tapping the Deprived chip.
    public static async Task ExplainDeprivationAsync(ICharAttribDataService svc)
    {
        if (!svc.IsDeprived) return;

        string status = svc.DeprivedDays > 1
            ? $"You've been deprived for {svc.DeprivedDays} days. Each further day adds 1 Fatigue."
            : "You became deprived at the end of the last day. Another deprived day adds 1 Fatigue.";

        var missing = new List<string>();
        if (!svc.NeedsMet) missing.Add("meet your needs (Daily Needs button)");
        if (!svc.Rested) missing.Add("take a Long Rest");
        string todo = missing.Count == 0
            ? "You have both chips, so you'll be clear shortly."
            : "To clear it today: " + string.Join(" and ", missing) + ". Rest after meeting your Daily Needs to get the restorative benefits(No one likes sleeping hungry), and you won't need to repeat either one today.";

        await Application.Current!.MainPage!.DisplayAlertAsync("Deprived",
            "While deprived you can't recover Hit Protection or ability scores.\n\n" + status + "\n\n" + todo, "OK");
    }
}