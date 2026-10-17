using System.Collections.Generic;

namespace CharManJur.Models;

// Generic elements a natural weapon can carry. The numbers match the order of the element picker (Ice = 1 ... Lightning = 6).
public enum DamageElement
{
    None = 0,
    Ice = 1,
    Fire = 2,
    Acid = 3,
    Wind = 4,
    Earth = 5,
    Lightning = 6
}

// A natural weapon a form wields while transformed. Named after the trait that gives it.
public class FormWeapon
{
    public string Name { get; set; } = string.Empty;
    public int Die { get; set; } = 6;                       // 4, 6, 8, 10, 12
    public WeaponCategoryType? Type { get; set; }           // Piercing / Bludgeoning / Slashing ...
    public List<WeaponEffectType> Effects { get; set; } = new();
    public DamageElement Element { get; set; }
}

// A Shifter trait. PD, TraitName and Effect are all a trait must have; every other field is optional, so a
// custom trait can set as many or as few of them as it likes.
public class ClassTrait
{
    public int PD { get; set; }                       // Polymorph Dice that must be invested to access the trait
    public string TraitName { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;

    // ----- Scores -----
    public int? Vigor { get; set; }                   // sets the form's VIG
    public int? Agility { get; set; }                 // sets the form's AGI

    // ----- Armor (the trait's name is shown as the armor's name; the cap of 3 still applies) -----
    public int Armor { get; set; }

    // ----- Natural weapon -----
    public int? WeaponDie { get; set; }               // 4, 6, 8, 10, 12. Null = this trait gives no weapon
    public WeaponCategoryType? WeaponType { get; set; }
    public bool Blast { get; set; }                   // the only weapon effect a trait can have: hits an area
    public DamageElement Element { get; set; }        // a fixed element
    public bool ChooseElement { get; set; }           // the player picks the element when transforming

    // ----- Duration (foundation traits only) -----
    public int? DurationMinutes { get; set; }

    // ----- Carrying -----
    // The form gets its own pack (capacity only from equipped containers, like a familiar). Reverting moves it back.
    public bool GrantsPack { get; set; }

    // ----- Reference only (not enforced) -----
    public int? CarrySlots { get; set; }

    // ----- Form rules (guidelines: they warn, never block) -----
    public bool IsSmall { get; set; }
    public bool IsLarge { get; set; }
    public bool RequiresSmall { get; set; }
    public bool ForbidsLarge { get; set; }
}