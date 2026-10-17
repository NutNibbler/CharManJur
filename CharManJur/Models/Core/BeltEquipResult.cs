namespace CharManJur.Models;

public enum BeltEquipResult
{
    Equipped,         // placed on the belt
    NeedsSlotChoice,  // belt has no free spot; the player must choose what to replace
    NotEligible       // jewelry, body armor, or a depleted item still in the pack
}