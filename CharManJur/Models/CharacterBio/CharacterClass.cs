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
    public List<ClassTrait> Traits { get; set; } = new();
    public string? ImageUrl { get; set; }  // Future use
    public int HitProtectionBonus { get; set; }
    public int FatigueBarBonus { get; set;  }

    // Dice pool this class grants: re-usable dice. The pool size is times picked x DicePerSelection.
    public DicePool? GrantsDicePool { get; set; }
    public int DicePerSelection { get; set; } = 1;
    public int FavourPerSelection { get; set; }   // Favour gained each time the class is picked
}
