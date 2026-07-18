using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class Spell
{
    public int Id { get; set; }
    public int? UnlockFeatureId { get; set; } = 19;
    public int? UnlockClassId { get; set; } = 0;

    public string SpellName { get; set; } = string.Empty;
    public string? SpellDescription { get; set; } = string.Empty;
    public string? SpellDice { get; set; } = string.Empty;
    public string? SpellRange { get; set; } = string.Empty;
}
