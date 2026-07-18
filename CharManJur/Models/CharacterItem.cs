using System;

namespace CharManJur.Models;

/// <summary>
/// An item instance belonging to a specific character.
/// Stores per-instance metadata separate from the item template.
/// </summary>
public class CharacterItem
{
    // === INSTANCE ID ===
    public int Id { get; set; }
    public Guid InstanceId { get; set; } = Guid.NewGuid();

    // === REFERENCE ===
    public int TemplateId { get; set; }

    // === CHARACTER OWNERSHIP ===
    public int CharacterId { get; set; }

    // === INSTANCE STATE ===
    public int Quantity { get; set; } = 1;
    public int RemainingUses { get; set; } = 0;
    public bool IsEquipped { get; set; } = false;

    // ===== BACKING FIELD FOR IsEmpty =====
    private bool _isEmpty = false;

    public bool IsEmpty
    {
        get
        {
            // Only items with uses can be empty
            if (Template?.HasUses != true) return false;
            return RemainingUses <= 0;
        }
        set
        {
            // Only allow setting empty for items with uses
            if (Template?.HasUses == true)
            {
                _isEmpty = value;
            }
        }
    }

    public bool IsIdentified { get; set; } = true;

    // === EQUIPMENT SLOTS ===
    public EquipmentSlotType EquipmentSlot { get; set; } = EquipmentSlotType.None;
    public int? SlotIndex { get; set; }      // 1-2 for Hands, 1-4 for Belt, 1 for Armor

    // === PLAYER CUSTOMIZATION ===
    public string? CustomName { get; set; }
    public string? PlayerNote { get; set; }

    // === TIMESTAMPS ===
    public DateTime AcquiredAt { get; set; } = DateTime.Now;
    public DateTime? LastModified { get; set; }

    // === DROPPED ITEMS (Archive) ===
    public DateTime? DroppedAt { get; set; }
    public bool IsDropped { get; set; } = false;

    // === SYNC (for multiplayer) ===
    public string? SyncId { get; set; }

    // === RESOLVED TEMPLATE (loaded at runtime) ===
    public Item? Template { get; set; }

    // === HELPER PROPERTIES ===
    public string DisplayName => CustomName ?? Template?.Name ?? $"Item {TemplateId}";
    public string DisplayDescription => Template?.BaseDescription ?? "Unknown item";

    public bool IsShield => Template?.IsShield == true;
    public bool IsArmor => Template?.IsArmor == true;
    public bool IsWeapon => Template?.IsWeapon == true;
    public bool IsJewelry => Template?.IsJewelry == true;
    public bool IsShieldOrWeapon => IsShield || IsWeapon;
    public int SlotsRequired => Template?.SlotsRequired ?? 1;

    // ===== CHECK IF ITEM IS EMPTY =====
    public bool IsActuallyEmpty()
    {
        // Only items with uses can be empty
        if (Template?.HasUses != true) return false;
        return RemainingUses <= 0;
    }
}

public enum EquipmentSlotType
{
    None,
    Hand,
    Belt,
    Armor
}