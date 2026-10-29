using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public static class EquipmentActionHelper
{
    public static bool CanEquipToHand(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (item.IsWorn) return false;
        if (item.IsArmor && !item.IsShield) return false;
        if (item.EquipmentSlot == EquipmentSlotType.Hand) return false;
        if (item.SlotsRequired > 1 && !charDataService.LimbSets.Any(l => l.PairType == LimbPairType.Paired))
            return false;
        return true;
    }

    public static bool CanEquipToBelt(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (item.IsWorn) return false;
        if (item.IsArmor && !item.IsShield) return false;
        if (item.EquipmentSlot == EquipmentSlotType.Belt) return false;
        return true;
    }

    // Why this item can't be equipped at all, or null if nothing blocks it.
    // An item that is already equipped may always be moved, even if it is out of uses.
    private static string? GetBlockedReason(CharacterItem item)
    {
        if (item.IsWorn) return "Jewelry is worn, not held or belted.";
        if (item.IsArmor && !item.IsShield) return "Body armor can only be worn in the armor slot.";
        if (item.IsEmpty && !item.IsEquipped)
            return $"'{item.DisplayName}' is out of uses, so it can't be equipped. Restore its uses first.";
        return null;
    }

    public static async Task<bool> EquipToHandAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        string? blocked = GetBlockedReason(item);
        if (blocked != null)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", blocked, "OK");
            return false;
        }

        if (item.SlotsRequired > 1)
        {
            var pairedSets = charDataService.LimbSets.Where(l => l.PairType == LimbPairType.Paired).ToList();
            if (pairedSets.Count == 0)
            {
                await mainPage.DisplayAlertAsync("Cannot Equip",
                    "This item requires two hands, and no paired limb set exists to hold it.", "OK");
                return false;
            }

            var freeSets = pairedSets.Where(l =>
                !l.IsOccupiedByTwoHandedItem &&
                l.SlotIndices.All(idx => charDataService.GetEquippedHandSlot(idx + 1) == null)).ToList();

            int targetLimbSetId;

            if (freeSets.Count == 1)
            {
                targetLimbSetId = freeSets[0].Id;
            }
            else if (freeSets.Count > 1)
            {
                var options = freeSets.Select(l => l.DisplayName).ToList();
                options.Add("Cancel");
                string? selected = await mainPage.DisplayActionSheetAsync("Select Limb Set", "Cancel", null, options.ToArray());
                if (selected == null || selected == "Cancel") return false;
                targetLimbSetId = freeSets.First(l => l.DisplayName == selected).Id;
            }
            else if (pairedSets.Count == 1)
            {
                targetLimbSetId = pairedSets[0].Id;   // only one choice: it swaps with whatever is there
            }
            else
            {
                var options = pairedSets.Select(l => l.DisplayName).ToList();
                options.Add("Cancel");
                string? selected = await mainPage.DisplayActionSheetAsync(
                    "Both hands are full. Which set should be freed?", "Cancel", null, options.ToArray());
                if (selected == null || selected == "Cancel") return false;
                targetLimbSetId = pairedSets.First(l => l.DisplayName == selected).Id;
            }

            if (!charDataService.EquipTwoHandedItemWithSwap(item.Id, targetLimbSetId))
            {
                await mainPage.DisplayAlertAsync("Cannot Equip",
                    $"'{item.DisplayName}' couldn't be placed in that hand set.", "OK");
                return false;
            }

            return true;
        }

        if (!charDataService.EquipToHandAuto(item.Id))
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", "No hand slot is available for this item.", "OK");
            return false;
        }

        return true;
    }

    public static async Task<bool> EquipToBeltAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        string? blocked = GetBlockedReason(item);
        if (blocked != null)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", blocked, "OK");
            return false;
        }

        switch (charDataService.TryEquipToBelt(item.Id))
        {
            case BeltEquipResult.Equipped:
                return true;

            case BeltEquipResult.NeedsSlotChoice:
                var popup = new BeltSlotSwapPopup(charDataService, item);
                await mainPage.Navigation.PushModalAsync(popup);
                bool? result = await popup.Completion;
                return result == true;

            default:
                await mainPage.DisplayAlertAsync("Cannot Equip", $"'{item.DisplayName}' can't be belted.", "OK");
                return false;
        }
    }

    // ===================================================================
    // DRAG & DROP
    // Can... methods only drive the gold/red highlight while dragging.
    // Drop... methods do the work and explain any refusal.
    // ===================================================================

    // An item in a dropped pack can't be used; equipped items can still be moved.
    private static bool PackLocked(CharacterItem item, ICharAttribDataService charDataService)
        => charDataService.IsPackDropped && !item.IsEquipped;

    private static string? GetDropBlockedReason(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (PackLocked(item, charDataService))
            return "Your pack is dropped, so items in it can't be used.";
        return GetBlockedReason(item);
    }

    public static bool CanDropOnHandSlot(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (GetDropBlockedReason(item, charDataService) != null) return false;
        return item.SlotsRequired < 2 || charDataService.LimbSets.Any(l => l.PairType == LimbPairType.Paired);
    }

    public static bool CanDropOnBeltSlot(CharacterItem item, ICharAttribDataService charDataService)
        => GetDropBlockedReason(item, charDataService) == null;

    public static bool CanDropOnArmor(CharacterItem item, ICharAttribDataService charDataService)
        => !PackLocked(item, charDataService)
           && item.IsArmor && !item.IsShield
           && !(item.IsEmpty && !item.IsEquipped);

    public static bool CanDropOnPack(CharacterItem item, ICharAttribDataService charDataService)
        => item.IsEquipped && !charDataService.IsPackDropped;

    public static async Task<bool> DropOnHandSlotAsync(CharacterItem item, int handSlot, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        string? blocked = GetDropBlockedReason(item, charDataService);
        if (blocked != null)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", blocked, "OK");
            return false;
        }

        if (item.SlotsRequired > 1 && !charDataService.LimbSets.Any(l => l.PairType == LimbPairType.Paired))
        {
            await mainPage.DisplayAlertAsync("Cannot Equip",
                "This item requires two hands, and no paired limb set exists to hold it.", "OK");
            return false;
        }

        if (!charDataService.EquipToHandSlotWithSwap(item.Id, handSlot))
        {
            await mainPage.DisplayAlertAsync("Cannot Equip",
                $"'{item.DisplayName}' couldn't be placed in that slot.", "OK");
            return false;
        }

        return true;
    }

    public static async Task<bool> DropOnBeltSlotAsync(CharacterItem item, int beltSlot, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        string? blocked = GetDropBlockedReason(item, charDataService);
        if (blocked != null)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", blocked, "OK");
            return false;
        }

        // A bulky item dropped on the last slot settles one slot earlier
        int startSlot = Math.Min(beltSlot, 5 - item.SlotsRequired);

        // From the hands: belt items trade places with it. From anywhere else: displaced items go to the pack.
        bool placed = item.EquipmentSlot == EquipmentSlotType.Hand
            ? charDataService.EquipToBeltWithSwap(item.Id, beltSlot)
            : charDataService.EquipToBeltAtSlot(item.Id, startSlot);

        if (!placed)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", $"'{item.DisplayName}' can't be belted there.", "OK");
            return false;
        }

        return true;
    }

    public static async Task<bool> DropOnArmorAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        if (PackLocked(item, charDataService))
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", "Your pack is dropped, so items in it can't be used.", "OK");
            return false;
        }

        if (!item.IsArmor || item.IsShield)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip", "Only body armor can be worn in the armor slot.", "OK");
            return false;
        }

        if (item.IsEmpty && !item.IsEquipped)
        {
            await mainPage.DisplayAlertAsync("Cannot Equip",
                $"'{item.DisplayName}' is out of uses, so it can't be equipped. Restore its uses first.", "OK");
            return false;
        }

        charDataService.EquipItemAsArmor(item.Id);
        return item.IsEquipped && item.EquipmentSlot == EquipmentSlotType.Armor;
    }

    public static async Task<bool> DropOnPackAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (!item.IsEquipped) return false;   // already in the pack

        if (charDataService.IsPackDropped)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Pack Dropped", "Pick up your pack before putting items back into it.", "OK");
            return false;
        }

        charDataService.UnequipItem(item.Id);
        return true;
    }

    // ===================================================================
    // ARMOR + UNEQUIP (shared by the item flyout and the item details panel)
    // ===================================================================

    public static bool CanEquipAsArmor(CharacterItem item, ICharAttribDataService charDataService)
        => item.IsArmor && !item.IsShield && !item.IsEquipped && !item.IsEmpty && !charDataService.IsPackDropped;

    public static async Task<bool> EquipAsArmorAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        var mainPage = Application.Current!.MainPage!;

        var existingArmor = charDataService.GetEquippedArmor();
        if (existingArmor != null && existingArmor.Id != item.Id)
        {
            bool replace = await mainPage.DisplayAlertAsync(
                "Armor Slot Occupied",
                $"You already have '{existingArmor.DisplayName}' equipped. Replace it?",
                "Yes, Replace",
                "Cancel");
            if (!replace) return false;
        }

        charDataService.EquipItemAsArmor(item.Id);
        return item.IsEquipped && item.EquipmentSlot == EquipmentSlotType.Armor;
    }

    // While the pack is dropped an unequipped item has nowhere to go, so it is dropped instead
    // (and lands in Recover like any other drop), after a warning. True if anything changed.
    public static async Task<bool> UnequipAsync(CharacterItem item, ICharAttribDataService charDataService)
    {
        if (!item.IsEquipped) return false;

        if (charDataService.IsPackDropped)
        {
            bool drop = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Pack Dropped",
                $"Your pack is dropped, so '{item.DisplayName}' has nowhere to go. Unequipping it will DROP it. You can get it back from Recover.",
                "Unequip & Drop",
                "Cancel");
            if (!drop) return false;

            charDataService.DropItem(item.Id);   // unequips it and logs a single "Item Dropped"
            return true;
        }

        charDataService.UnequipItem(item.Id);
        return true;
    }
}