using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class Hinderance : INotifyPropertyChanged
{
    // === CUSTOM HINDERANCE ID CONSTANTS ===
    public const int CUSTOM_HINDERANCE_BASE = 70000;   // Base for custom hinderance IDs
    public const int CUSTOM_HINDERANCE_START = 70001;  // First custom hinderance ID

    // === CUSTOM HINDERANCE ATTRIBUTES ===
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
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPlayerCreated { get; set; } = false;

    // === ABILITY SCORE MODIFIERS ===
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;

    // === SKILL MODIFIERS ===
    public List<HinderanceSkillModifier> SkillModifiers { get; set; } = new();

    // === REWARD OPTIONS ===
    public bool AllowsStatBonus { get; set; } = true;
    public bool AllowsTrainingPoint { get; set; } = true;

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

    public int GetSkillModifier(string skillName)
    {
        var modifier = SkillModifiers.FirstOrDefault(s => s.SkillName == skillName);
        return modifier?.Modifier ?? 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class HinderanceSkillModifier
{
    public string SkillName { get; set; } = string.Empty;
    public int Modifier { get; set; }
}

public enum HinderanceRewardType
{
    None,
    StatBonus,
    TrainingPoint
}