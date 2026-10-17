using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class Race : INotifyPropertyChanged
{
    // === CUSTOM RACE ID CONSTANTS ===
    public const int CUSTOM_RACE_BASE = 90000;   // Base for custom race IDs
    public const int CUSTOM_RACE_START = 90001;  // First custom race ID

    // === CUSTOM RACE ATTRIBUTES ===
    public Guid Guid { get; set; } = Guid.Empty;  // permanent identity — never changes, this is what actually gets referenced

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
    public string RaceNameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> CompatibleCampaigns { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string FeatureDescription { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int HitProtectionBonus { get; set; }

    // === ABILITY SCORE MODIFIERS ===
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;

    // === SKILL MODIFIERS ===
    public List<RaceSkillBonus> SkillBonuses { get; set; } = new();

    // ===== PREHENSILE LIMBS =====
    public List<PrehensileLimbSet> LimbSets { get; set; } = new();

    // === HELPER METHODS ===
    public int GetAbilityModifier(string statName)
    {
        return statName.ToLower() switch
        {
            "vigor" => VigorModifier,
            "agility" => AgilityModifier,
            "mind" => MindModifier,
            "spirit" => SpiritModifier,
            _ => 0
        };
    }

    public int GetSkillBonus(string skillName)
    {
        var bonus = SkillBonuses.FirstOrDefault(s => s.SkillName == skillName);
        return bonus?.Bonus ?? 0;
    }

    public int GetTotalLimbSlots()
    {
        int total = 0;
        foreach (var set in LimbSets)
        {
            total += set.SlotCount;
        }
        return total;
    }

    public Race Clone()
    {
        // Deliberately NOT copying Guid/SourcePackId/IsLoaded — a clone is a brand-new
        // custom race, not a copy tied to the source's pack/identity. Same reasoning as
        // CharacterBackground.Clone() and AssetPackService's Copy-To duplication.
        return new Race
        {
            Id = Id,
            RaceNameId = RaceNameId,
            Name = Name,
            CompatibleCampaigns = new List<string>(CompatibleCampaigns),
            Description = Description,
            FeatureName = FeatureName,
            FeatureDescription = FeatureDescription,
            ImageUrl = ImageUrl,
            HitProtectionBonus = HitProtectionBonus,
            VigorModifier = VigorModifier,
            AgilityModifier = AgilityModifier,
            MindModifier = MindModifier,
            SpiritModifier = SpiritModifier,
            SkillBonuses = SkillBonuses.Select(s => new RaceSkillBonus { SkillName = s.SkillName, Bonus = s.Bonus }).ToList(),
            LimbSets = LimbSets.Select(l => new PrehensileLimbSet
            {
                Id = l.Id,
                Name = l.Name,
                PairType = l.PairType,
                SlotCount = l.SlotCount,
                DisplayName = l.DisplayName,
                Icon = l.Icon,
                LeftSlotName = l.LeftSlotName,
                RightSlotName = l.RightSlotName,
                SlotIndices = new List<int>(l.SlotIndices),
                TwoHandedItemId = l.TwoHandedItemId
            }).ToList()
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RaceSkillBonus
{
    public string SkillName { get; set; } = string.Empty;
    public int Bonus { get; set; }
}

// ===== PREHENSILE LIMB SYSTEM =====
public class PrehensileLimbSet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LimbPairType PairType { get; set; } = LimbPairType.Paired;
    public int SlotCount { get; set; } = 2; // 2 for paired, 1 for single
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = "🫳"; // Default hand icon

    // ===== CUSTOM NAMING FOR INDIVIDUAL SLOTS =====
    public string LeftSlotName { get; set; } = "Left";
    public string RightSlotName { get; set; } = "Right";

    // Which slot indices this limb set occupies (e.g., 0,1 for first hands, 2,3 for second)
    public List<int> SlotIndices { get; set; } = new();

    // ===== TWO-HANDED ITEM TRACKING =====
    public int? TwoHandedItemId { get; set; }
    public bool IsOccupiedByTwoHandedItem => TwoHandedItemId.HasValue;
}

public enum LimbPairType
{
    Paired,  // Two slots, can equip two-handed items
    Single   // One slot, cannot equip two-handed items
}