using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class CharacterClass
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string RecurringBenefit { get; set; } = string.Empty;
    public List<string> CompatibleCampaigns { get; set; } = new();
    public string LevelingSkillBonus { get; set; } = string.Empty;

    public List<SubFeatureType> ClassUnlockableTypes { get; set; } = new();
    public List<ClassFeature> Features { get; set; } = new();
    public string? ImageUrl { get; set; }  // Future use
    public int HitProtectionBonus { get; set; }  // Example stat bonuses
}
