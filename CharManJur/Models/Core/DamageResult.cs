using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

// Outcome of applying damage: HP absorbs first, any excess reduces Vigor ("Critical Damage").
public class DamageResult
{
    public int HitProtectionLost { get; init; }
    public int VigorLost { get; init; }
    public bool TriggersVigorSave { get; init; }
    public bool IsDead { get; init; }
    public int ArmorMitigated { get; init; }
}