using CharManJur.Models;

namespace CharManJur.Services;

// Builds the live stat chips for the GM's card from a character that is in play right now.
// Same chips and rules as GmCharacterService.GetSummaryAsync builds for test players.
public static class CharacterSummaryBuilder
{
    public static CharacterSummaryMessage Build(ICharAttribDataService svc, Guid characterGuid)
    {
        // An encumbered character's Max HP is 0 (the live game applies that when its screen loads).
        bool encumbered = svc.IsEncumbered;
        int maxHp = encumbered ? 0 : svc.Hitpoints;
        int hp = encumbered ? 0 : svc.CurrentHitpoints;

        int fatigue = svc.FatigueTotal, fatigueMax = svc.FatigueBarMax, overflow = svc.FatigueInInventory;
        int used = svc.InventorySlotsUsed, room = svc.InventorySlotsAvailable;

        // While transformed, damage hits Stability and the form drops before HP is touched, so Stability replaces HP.
        bool transformed = svc.Transform is { IsTransformed: true };
        int stability = transformed ? (int)svc.Transform!.StabilityCurrent : 0;
        int stabilityMax = transformed ? (int)svc.Transform!.StabilityMax : 0;

        var chips = new List<SummaryChip>
        {
            transformed
                ? Chip("🐺 STAB", $"{stability}/{stabilityMax}", Level(stability, stabilityMax))
                : Chip("HP", $"{hp}/{maxHp}", Level(hp, maxHp)),
            Chip("VIG", $"{svc.CurrentStatVigor}/{svc.TotalStatVigor}", Level(svc.CurrentStatVigor, svc.TotalStatVigor)),
            Chip("AGI", $"{svc.CurrentStatAgility}/{svc.TotalStatAgility}", Level(svc.CurrentStatAgility, svc.TotalStatAgility)),
            Chip("MND", $"{svc.CurrentStatMind}/{svc.TotalStatMind}", Level(svc.CurrentStatMind, svc.TotalStatMind)),
            Chip("SPI", $"{svc.CurrentStatSpirit}/{svc.TotalStatSpirit}", Level(svc.CurrentStatSpirit, svc.TotalStatSpirit)),
            Chip("Fatigue",
                overflow > 0 ? $"{fatigue}/{fatigueMax} (+{overflow} in pack)" : $"{fatigue}/{fatigueMax}",
                overflow > 0 ? 2 : fatigue > 0 ? 1 : 0),
            Chip("Pack", encumbered ? $"{used}/{room} ENCUMBERED" : $"{used}/{room}", encumbered ? 2 : 0)
        };

        int waiting = svc.PendingDamage.Count + svc.PendingEffects.Count;
        string compact = transformed
            ? $"🐺 STAB {stability}/{stabilityMax} · VIG {svc.CurrentStatVigor}/{svc.TotalStatVigor}"
            : $"HP {hp}/{maxHp} · VIG {svc.CurrentStatVigor}/{svc.TotalStatVigor}";
        if (waiting > 0) compact += $" · 📨 {waiting} waiting";

        return new CharacterSummaryMessage { CharacterGuid = characterGuid, Chips = chips, Compact = compact, Waiting = waiting };
    }

    // Two summaries with the same key look identical on the GM's card, so the second one is not sent.
    public static string Key(CharacterSummaryMessage summary)
        => summary.Compact + "|" + summary.Waiting + "|" + string.Join(";", summary.Chips.Select(c => $"{c.Label}={c.Value}/{c.Level}"));

    // 0 = full, 1 = below its max, 2 = gone.
    private static int Level(int current, int max) => current <= 0 ? 2 : current < max ? 1 : 0;

    private static SummaryChip Chip(string label, string value, int level) => new() { Label = label, Value = value, Level = level };
}