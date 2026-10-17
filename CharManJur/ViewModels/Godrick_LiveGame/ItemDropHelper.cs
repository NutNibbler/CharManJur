using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// Drop flow shared by Drop Mode and the item flyout. Stackable items with more than one
// in the stack ask how many to drop (default: all); everything else keeps the plain confirm.
public static class ItemDropHelper
{
    // Returns true when anything was dropped.
    public static async Task<bool> DropAsync(CharacterItem item, ICharAttribDataService svc)
    {
        bool askQuantity = item.Quantity > 1 && (item.Template?.IsStackableItem ?? false);

        if (askQuantity)
        {
            int? quantity = await ItemTransferHelper.PromptQuantityAsync(item, "🫳 Drop Item", "Drop");
            if (quantity == null) return false;

            svc.DropItem(item.Id, quantity.Value);
            return true;
        }

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "🫳 Drop Item",
            $"Are you sure you want to drop: '{item.DisplayName}'?",
            "Yes, Drop",
            "Cancel");
        if (!confirm) return false;

        svc.DropItem(item.Id);
        return true;
    }
}