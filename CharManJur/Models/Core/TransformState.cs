using System;
using System.Collections.Generic;

namespace CharManJur.Models;

// A Shifter's current transformation. Saved with the character; null (or IsTransformed false) = normal form.
public class TransformState
{
    public bool IsTransformed { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PolymorphDice { get; set; }
    public int StabilityMax { get; set; }       // the sum rolled
    public int StabilityCurrent { get; set; }
    public int FormVigor { get; set; } = 10;
    public int FormAgility { get; set; } = 10;
    public int Armor { get; set; }
    public string? ArmorName { get; set; }      // the armoring trait's name, shown on the equipment dashboard
    public List<FormWeapon> Weapons { get; set; } = new();   // one per weapon trait
    public int DurationMinutes { get; set; } = 10;
    public List<string> Traits { get; set; } = new();    // names, so the form survives even if a trait is later removed
    public DateTime StartedAt { get; set; } = DateTime.Now;

    // The ability loss the character had before transforming. Reverting puts it back.
    public int PreVigorAdjustment { get; set; }
    public int PreAgilityAdjustment { get; set; }
}