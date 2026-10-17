using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CharManJur.Models;

public class Familiar : INotifyPropertyChanged
{
    // === CUSTOM FAMILIAR ID CONSTANTS ===
    public const int CUSTOM_FAMILIAR_BASE = 80000;   // Base for custom familiar IDs
    public const int CUSTOM_FAMILIAR_START = 80001;  // First custom familiar ID

    // === CUSTOM FAMILIAR ATTRIBUTES ===
    public Guid Guid { get; set; } = Guid.Empty;  // permanent identity of the TEMPLATE

    [JsonIgnore] public bool HasGivenName => !string.IsNullOrWhiteSpace(FmlrPlayerName);

    // Picker text: "Given Name - Base Name", or just the base name.
    [JsonIgnore]
    public string DisplayName => HasGivenName
        ? $"{FmlrPlayerName} - {FmlrName}"
        : (FmlrName ?? "(unnamed)");

    private bool _isLoaded = true;
    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (_isLoaded != value)
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }
    }

    public string SourcePackId { get; set; } = "Local";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int Id { get; set; }
    public int HP { get; set; }
    public int StatVigor { get; set; }
    public int StatAgility { get; set; }
    public int StatMind { get; set; }
    public int StatSpirit { get; set; }

    public FmlrSizes FmlrSize { get; set; }
    public string? FmlrPlayerName { get; set; }
    public string? FmlrName { get; set; }
    public string? FmlrDescription { get; set; }
    public FmlrClasses? FmlrClass { get; set; }
    public FmlrIntelligences? FmlrIntelligence { get; set; }

    public bool IsPlayerCreated { get; set; } = false;
    public int? CreatedByPlayerId { get; set; }
    public string? SourceCampaignId { get; set; }
    public bool IsFoundation { get; set; } = true;
    public bool IsFormPack { get; set; }   // a Shifter form's temporary pack; it goes away when the form ends

    // ===== WEAPON DATA =====
    public string? FmlrWeaponName { get; set; }
    public string? FmlrWeaponType { get; set; } = "Generic";
    public FmlrWeaponSpeeds? FmlrWeaponSpeed { get; set; }
    public FmlrWeaponDamageDies? FmlrWeaponDamageDie { get; set; }

    public List<string>? Abilities { get; set; }

    [JsonIgnore]
    public string WeaponSummary =>
        string.IsNullOrWhiteSpace(FmlrWeaponName)
            ? "No natural weapon"
            : $"{FmlrWeaponName} — {FmlrWeaponSpeed?.ToString() ?? "Balanced"}, {FmlrWeaponDamageDie?.ToString() ?? "—"}";

    // ===== LIVE STATE (belongs to the character's copy, not the template) =====
    public Guid InstanceId { get; set; } = Guid.Empty;
    public int HitpointsDamage { get; set; }
    public int FatigueCount { get; set; }
    public int VigorDamage { get; set; }
    public int AgilityDamage { get; set; }
    public int MindDamage { get; set; }
    public int SpiritDamage { get; set; }
    public List<CharacterItem> Inventory { get; set; } = new();

    [JsonIgnore] public int CurrentHP => IsEncumbered ? 0 : Math.Max(0, HP - HitpointsDamage);
    [JsonIgnore] public int CurrentVigor => Math.Max(0, StatVigor - VigorDamage);
    [JsonIgnore] public int CurrentAgility => Math.Max(0, StatAgility - AgilityDamage);
    [JsonIgnore] public int CurrentMind => Math.Max(0, StatMind - MindDamage);
    [JsonIgnore] public int CurrentSpirit => Math.Max(0, StatSpirit - SpiritDamage);

    // Dead when Vigor is reduced to 0. The familiar stays until the player removes it.
    [JsonIgnore] public bool IsDead => !IsFormPack && VigorDamage > 0 && CurrentVigor <= 0;

    // delta > 0 raises the current value, delta < 0 lowers it.
    public void AdjustStat(FamiliarStat stat, int delta)
    {
        switch (stat)
        {
            case FamiliarStat.Vigor: VigorDamage = Math.Clamp(VigorDamage - delta, 0, StatVigor); break;
            case FamiliarStat.Agility: AgilityDamage = Math.Clamp(AgilityDamage - delta, 0, StatAgility); break;
            case FamiliarStat.Mind: MindDamage = Math.Clamp(MindDamage - delta, 0, StatMind); break;
            case FamiliarStat.Spirit: SpiritDamage = Math.Clamp(SpiritDamage - delta, 0, StatSpirit); break;
        }
    }

    public void AdjustHitpoints(int delta) => HitpointsDamage = Math.Clamp(HitpointsDamage - delta, 0, HP);

    // Abilities and HP back to full. Fatigue is left alone.
    public void RestoreAll()
    {
        VigorDamage = AgilityDamage = MindDamage = SpiritDamage = 0;
        HitpointsDamage = 0;
    }

    // ===== ABILITY MODIFIERS (rulebook table: 1=-5, 2-3=-4 ... 18-19=+4, 20=+5) =====
    public static int AbilityModifierFor(int score)
    {
        int clamped = Math.Clamp(score, 1, 20);
        int modifier = (clamped - 10) / 2;
        if (clamped < 10 && (clamped - 10) % 2 != 0) modifier--;
        return Math.Clamp(modifier, -5, 5);
    }

    [JsonIgnore] public int VigorModifier => AbilityModifierFor(CurrentVigor);
    [JsonIgnore] public int AgilityModifier => AbilityModifierFor(CurrentAgility);
    [JsonIgnore] public int MindModifier => AbilityModifierFor(CurrentMind);
    [JsonIgnore] public int SpiritModifier => AbilityModifierFor(CurrentSpirit);

    // ===== FATIGUE (same rule as characters: bar = AGI modifier, minimum 1) =====
    [JsonIgnore] public int FatigueBarMax => Math.Max(1, AgilityModifier);
    [JsonIgnore] public int FatigueInBar => Math.Min(FatigueCount, FatigueBarMax);
    [JsonIgnore] public int FatigueOverflow => Math.Max(0, FatigueCount - FatigueBarMax);
    // Fatigue past the bar still eats pack space (floored at 0 by AvailableSlots).
    [JsonIgnore] public int FatigueInInventory => Math.Max(0, FatigueCount - FatigueBarMax);

    // ===== CONTAINERS / CAPACITY =====
    // Slots a familiar carries without any container. Animals have none (0); a crafted construct has its
    // own. Equipped Container items add to this.
    public int BaseInventoryCapacity { get; set; }
    [JsonIgnore]
    public IEnumerable<CharacterItem> EquippedContainers => Inventory.Where(i =>
        i.IsEquipped && !i.IsDropped && !i.IsEmpty && i.Template is { IsContainer: true });

    [JsonIgnore]
    public int InventorySlotsTotal =>
    BaseInventoryCapacity + EquippedContainers.Sum(i => i.Template!.ContainerCapacity * Math.Max(1, i.Quantity));

    [JsonIgnore]
    public int InventorySlotsAvailable =>
        InventorySlotCalculator.AvailableSlots(InventorySlotsTotal, FatigueInInventory);

    // Carried = everything NOT equipped. Same Petty / Regular / Bulky slot rules as characters.
    [JsonIgnore]
    public int InventorySlotsUsed => Inventory
        .Where(i => !i.IsDropped && !i.IsEmpty && !i.IsEquipped && i.Template != null)
        .Sum(i => InventorySlotCalculator.SlotsFor(i.Template!, i.Quantity));

    // Over capacity = encumbered, and encumbrance zeroes HP (damage is kept, so HP comes back when relieved).
    [JsonIgnore] public bool IsEncumbered => InventorySlotsUsed > InventorySlotsAvailable;

    // ===== INSTANCES =====
    public void EnsureInstanceId()
    {
        if (InstanceId == Guid.Empty) InstanceId = Guid.NewGuid();
    }

    // A fresh character-owned copy of a template: new identity, empty pack, no fatigue, no damage.
    public Familiar CreateInstanceCopy()
    {
        var copy = (Familiar)MemberwiseClone();
        copy.PropertyChanged = null;
        copy.InstanceId = Guid.NewGuid();
        copy.Inventory = new List<CharacterItem>();
        copy.HitpointsDamage = 0;
        copy.FatigueCount = 0;
        copy.VigorDamage = copy.AgilityDamage = copy.MindDamage = copy.SpiritDamage = 0;
        copy.FmlrPlayerName = null;
        copy.Abilities = Abilities != null ? new List<string>(Abilities) : null;
        return copy;
    }

    // ===== INVENTORY OPERATIONS =====
    // Same record rules as characters: top up partial stacks, then create new legal records.
    private int NextItemId() => Inventory.Count > 0 ? Inventory.Max(i => i.Id) + 1 : 1;

    // Bestowed straight from an item template (GM bestow).
    // Bestowed straight from an item template (GM bestow).
    public bool AddItemToInventory(Item template, int quantity = 1, int? writtenValueInChips = null, int? startingUses = null)
    {
        if (template == null || quantity <= 0) return false;

        // Full uses unless a lower number was chosen; only a single item can start partly used.
        int fullUses = template.HasUses ? template.Uses!.Value : 0;
        int remainingUses = template.HasUses && startingUses.HasValue && !(template.IsStackableItem && quantity > 1)
            ? Math.Clamp(startingUses.Value, 0, fullUses)
            : fullUses;

        var source = new CharacterItem
        {
            TemplateId = template.Id,
            TemplateGuid = template.IsPlayerCreated ? template.Guid : null,
            Template = template,
            Quantity = quantity,
            RemainingUses = remainingUses,
            IsEmpty = template.HasUses && remainingUses <= 0,
            WrittenValueInChips = template.IsValueEditable ? writtenValueInChips : null
        };
        ItemRecordTransfer.MoveInto(Inventory, source, quantity, NextItemId);
        return true;
    }

    // Handed over from a character's pack: keeps uses, notes and custom name.
    public void ReceiveItem(CharacterItem source, int quantity)
    {
        if (IsDead) return;
        ItemRecordTransfer.MoveInto(Inventory, source, quantity, NextItemId);
    }

    public bool RemoveItemFromInventory(int characterItemId)
    {
        var item = Inventory.FirstOrDefault(i => i.Id == characterItemId);
        return item != null && Inventory.Remove(item);
    }

    // Equipping has NO limit (soft rules, player-centralized). Equipped items don't use pack slots.
    public bool SetEquipped(int characterItemId, bool equipped)
    {
        var item = Inventory.FirstOrDefault(i => i.Id == characterItemId && !i.IsDropped);
        if (item == null || item.IsEmpty || item.IsEquipped == equipped) return false;
        item.IsEquipped = equipped;
        item.LastModified = DateTime.Now;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum FmlrSizes
{
    [Display(Name = "Tiny", Description = "Roughly the size a of mouse or bird.")]
    Tiny,
    [Display(Name = "Small", Description = "Roughly the size a cat or dog.")]
    Small,
    [Display(Name = "Medium", Description = "About the size of a human.")]
    Medium,
    [Display(Name = "Large", Description = "Minotaur or Horse.")]
    Large,
    [Display(Name = "Giant", Description = "At least as large as an Elephant.")]
    Giant
}

public enum FamiliarStat
{
    Vigor,
    Agility,
    Mind,
    Spirit
}

public enum FmlrClasses
{
    Aves,
    Reptilia,
    Amphibia,
    Mammalia,
    Anthropoda,
    Mollusca,
    Annelida,
    Construct
}

public enum FmlrIntelligences
{
    [Display(Name = "Inanimate", Description = "Used this for familiars without any life force.")]
    Inanimate,
    [Display(Name = "Wild Animal", Description = "Typical animal intelligence for non-domestic types.")]
    Wild,
    [Display(Name = "Semi-domestic", Description = "Semi-intelligent with basic trainability.")]
    SemiDomestic,
    [Display(Name = "Domestic", Description = "Semi-intelligent with a high degree of trainability and obedience.")]
    Domestic,
    [Display(Name = "Sapient", Description = "Self-aware, highly intelligent.")]
    Sapient,
}

public enum FmlrWeaponSpeeds
{
    Slow,
    Balanced,
    Fast
}

public enum FmlrWeaponDamageDies
{
    D4,
    D6,
    D8,
    D10,
    D12,
    D20,
    D60,
    D100,
    D2000
}