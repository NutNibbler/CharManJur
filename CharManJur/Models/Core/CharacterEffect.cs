using System;
using System.Collections.Generic;

namespace CharManJur.Models;

public enum EffectTargetType
{
    VigorCurrent,
    VigorMax,
    AgilityCurrent,
    AgilityMax,
    MindCurrent,
    MindMax,
    SpiritCurrent,
    SpiritMax,
    HitpointsCurrent,
    HitpointsMax,
    Skill
}

public enum EffectDurationType
{
    Persistent,   // stays on the character until manually cleared
    OneShot       // applies once immediately, never stored
}

// Hardcoded effects are reminders the app itself attaches (not player-created via
// Apply Effect). Tapping one triggers special behavior instead of a plain "Clear?" prompt.
public enum HardcodedEffectType
{
    None,
    VigorSave,
    DeathsDoor,
    Transformed
}

public class EffectModifier
{
    public EffectTargetType Target { get; set; }
    public string? SkillName { get; set; }   // set only when Target == Skill
    public int Amount { get; set; }          // positive = buff, negative = debuff
}

public class CharacterEffect
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public EffectDurationType Duration { get; set; } = EffectDurationType.Persistent;
    public HardcodedEffectType HardcodedType { get; set; } = HardcodedEffectType.None;
    public bool IsHardcoded => HardcodedType != HardcodedEffectType.None;
    public DateTime AppliedAt { get; set; } = DateTime.Now;
    public List<EffectModifier> Modifiers { get; set; } = new();
}