using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CharManJur.Models;

public class Familiar
{
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

    // ===== WEAPON DATA =====
    public string? FmlrWeaponName { get; set; }
    public string? FmlrWeaponType { get; set; } = "Generic";
    public FmlrWeaponSpeeds? FmlrWeaponSpeed { get; set; }
    public FmlrWeaponDamageDies? FmlrWeaponDamageDie { get; set; }

    public List<string>? Abilities { get; set; }

    // ===== FAMILIAR INVENTORY =====
    public List<CharacterItem> Inventory { get; set; } = new();
    public int InventorySlotsUsed
    {
        get
        {
            int total = 0;
            foreach (var item in Inventory)
            {
                if (item.IsDropped) continue;
                int slotsPerItem = item.Template?.SlotsRequired ?? 1;
                total += item.Quantity * slotsPerItem;
            }
            return total;
        }
    }
    public int InventorySlotsTotal
    {
        get
        {
            int baseSlots = 4;
            int vigorASM = GetAbilityModifier(StatVigor);
            return baseSlots + vigorASM;
        }
    }
    public bool IsEncumbered => InventorySlotsUsed > InventorySlotsTotal;

    private int GetAbilityModifier(int score)
    {
        int clampedScore = Math.Clamp(score, 1, 20);
        int modifier = (clampedScore - 10) / 2;
        if (clampedScore < 10 && (clampedScore - 10) % 2 != 0)
        {
            modifier--;
        }
        return Math.Clamp(modifier, -5, 5);
    }

    public void AddItemToInventory(Item template, int quantity = 1)
    {
        if (template == null) return;

        if (template.IsStackableItem)
        {
            var existingStack = Inventory.FirstOrDefault(i =>
                i.TemplateId == template.Id &&
                !i.IsEquipped &&
                !i.IsEmpty &&
                i.Quantity < 99);

            if (existingStack != null)
            {
                existingStack.Quantity += quantity;
                existingStack.LastModified = DateTime.Now;
                return;
            }
        }

        var newItem = new CharacterItem
        {
            Id = Inventory.Count > 0 ? Inventory.Max(i => i.Id) + 1 : 1,
            TemplateId = template.Id,
            Template = template,
            Quantity = quantity,
            RemainingUses = template.HasUses ? template.Uses.Value : 0,
            IsEmpty = false,
            AcquiredAt = DateTime.Now
        };

        Inventory.Add(newItem);
    }

    public void RemoveItemFromInventory(int characterItemId)
    {
        var item = Inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            Inventory.Remove(item);
        }
    }
}

public enum FmlrSizes
{
    Tiny,
    Small,
    Medium,
    Large,
    Giant
}

public enum FmlrClasses
{
    Aves,
    Reptilia,
    Amphibia,
    Mammalia,
    Anthropoda,
    Mollusca,
    Annelida
}

public enum FmlrIntelligences
{
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