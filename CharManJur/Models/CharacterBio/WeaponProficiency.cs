using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CharManJur.Models;

// Everything the proficiency system saves with a character.
public class WeaponProficiencyState
{
    // False until the player has picked a starting damage type (asked once, on first load).
    public bool Initialized { get; set; }

    public List<DamageTypeProgress> DamageTypes { get; set; } = new();

    // One entry per weapon (by template, not by item), created by the first kill.
    public List<WeaponKillRecord> Weapons { get; set; } = new();

    // Weapons whose skills the Warrior benefit unlocks without kills. Only the identity fields matter here.
    public List<WeaponKillRecord> GrantedSkills { get; set; } = new();

    // The Warrior pick made during character creation (a level-up pick is not this one).
    [JsonIgnore]
    public WeaponKillRecord? CreationGrant => GrantedSkills.FirstOrDefault(r => r.FromCreation);

    // Character creation: replaces any earlier creation pick, so going back and forward never stacks them.
    // Passing null removes it (the player changed to another class).
    public void SetCreationGrant(Item? template)
    {
        GrantedSkills.RemoveAll(r => r.FromCreation);
        if (template != null && template.IsWeapon)
            GrantedSkills.Add(WeaponKillRecord.For(template, fromCreation: true));
    }
}

public class DamageTypeProgress
{
    public WeaponDamageType Type { get; set; }
    public int Kills { get; set; }          // 0-5
    public bool Granted { get; set; }       // proficient without kills (the starting choice)
}

public class WeaponKillRecord
{
    public int TemplateId { get; set; }     // foundation weapons
    public Guid? TemplateGuid { get; set; } // custom weapons (their numeric Id changes between runs)
    public string Name { get; set; } = string.Empty;
    public WeaponDamageType DamageType { get; set; }
    public int Kills { get; set; }          // 0-5
    public bool FromCreation { get; set; }  // a Warrior weapon chosen while creating the character

    public bool IsFor(Item template)
        => template.IsPlayerCreated
            ? TemplateGuid == template.Guid
            : !TemplateGuid.HasValue && TemplateId == template.Id;

    public static WeaponKillRecord For(Item template, bool fromCreation = false) => new()
    {
        TemplateId = template.Id,
        TemplateGuid = template.IsPlayerCreated ? template.Guid : null,
        Name = template.Name,
        DamageType = template.DamageType ?? WeaponDamageType.Piercing,
        FromCreation = fromCreation
    };
}