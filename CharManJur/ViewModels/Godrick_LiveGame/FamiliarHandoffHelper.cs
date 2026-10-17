using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// Handing an item to one of the character's own familiars.
public static class FamiliarHandoffHelper
{
    public static bool CanHand(CharacterItem? item, ICharAttribDataService svc) =>
        item != null
        && !item.IsEquipped
        && !svc.IsPackDropped
        && svc.AcquiredFamiliars.Any(f => !f.IsDead);

    private static async Task<bool> PassesGuardsAsync(CharacterItem item, ICharAttribDataService svc)
    {
        var page = Application.Current!.MainPage!;

        if (item.IsEquipped)
        {
            await page.DisplayAlertAsync("Unequip First", "Unequip this item before handing it over.", "OK");
            return false;
        }
        if (svc.IsPackDropped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "Pick up your pack before handing items over.", "OK");
            return false;
        }
        return true;
    }

    // Used by the details-panel button: picks the familiar, then hands over.
    public static async Task<bool> HandToFamiliarAsync(CharacterItem item, ICharAttribDataService svc)
    {
        if (!await PassesGuardsAsync(item, svc)) return false;

        var page = Application.Current!.MainPage!;
        var targets = svc.AcquiredFamiliars.Where(f => !f.IsDead).ToList();
        if (targets.Count == 0)
        {
            await page.DisplayAlertAsync("No One to Hand To",
                "No living familiar with a pack can receive items.", "OK");
            return false;
        }

        var target = targets[0];
        if (targets.Count > 1)
        {
            var labels = targets.Select((f, i) => $"{i + 1}. {f.DisplayName}").ToArray();
            string? picked = await page.DisplayActionSheetAsync("Hand to which familiar?", "Cancel", null, labels);
            int index = picked == null ? -1 : Array.IndexOf(labels, picked);
            if (index < 0) return false;
            target = targets[index];
        }

        return await HandToFamiliarAsync(item, svc, target);
    }

    // Used by the Transfer menu, which has already chosen the familiar.
    public static async Task<bool> HandToFamiliarAsync(CharacterItem item, ICharAttribDataService svc, Familiar target)
    {
        if (!await PassesGuardsAsync(item, svc)) return false;

        int? quantity = await ItemTransferHelper.PromptQuantityAsync(item, "Hand Over", "Hand over");
        if (quantity == null) return false;

        return svc.HandItemToFamiliar(item.Id, target.InstanceId, quantity.Value);
    }
}