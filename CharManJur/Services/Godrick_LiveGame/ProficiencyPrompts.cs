using CharManJur.Models;

namespace CharManJur.Services;

public static class ProficiencyPrompts
{
    // Asks which damage type the character starts proficient in. Returns true when a choice was made.
    public static async Task<bool> AskStartingTypeAsync(WeaponProficiencyTracker tracker, string cancelText = "Decide later")
    {
        var page = Application.Current?.MainPage;
        if (page == null) return false;

        var options = new Dictionary<string, WeaponDamageType>
        {
            ["Piercing: a maximum damage roll ignores armor"] = WeaponDamageType.Piercing,
            ["Bludgeoning: always ignores 1 armor"] = WeaponDamageType.Bludgeoning,
            ["Slashing: +1 damage against targets with 0 armor"] = WeaponDamageType.Slashing
        };

        string? choice = await page.DisplayActionSheetAsync(
            "Choose your starting damage type proficiency", cancelText, null, options.Keys.ToArray());

        if (choice == null || !options.TryGetValue(choice, out var type)) return false;

        await tracker.ChooseStartingTypeAsync(type);
        return true;
    }
}