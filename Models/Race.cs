using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class Race
{
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

    // Which slot indices this limb set occupies (e.g., 0,1 for first hands, 2,3 for second)
    public List<int> SlotIndices { get; set; } = new();
}

public enum LimbPairType
{
    Paired,  // Two slots, can equip two-handed items
    Single   // One slot, cannot equip two-handed items
}