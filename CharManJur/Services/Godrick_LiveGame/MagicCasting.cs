using CharManJur.Models;

namespace CharManJur.Services;

public sealed class CastRequest
{
    public SubFeatureEntry? Spell { get; init; }                 // null when sculpting a cantrip from techniques
    public IReadOnlyList<SubFeatureEntry> Techniques { get; init; } = Array.Empty<SubFeatureEntry>();
    public string? CombinedName { get; init; }                   // only when the player typed a name
    public MagicRollResult Roll { get; init; } = null!;
    public bool IgnoreDamage { get; init; }                      // the GM waived the series damage
}

// Applies a finished Magic Die roll to the character: fatigue, series backlash, and the log entries.
// Written against the public character service so the big service file stays untouched.
public class MagicCasting
{
    private readonly ICharAttribDataService _svc;
    private readonly IPlayerActionLogService _log;
    // The Magic Dice pool: the most dice that can be invested in one cast.
    public int MagicDicePool => _svc.GetDicePoolSize(DicePool.Magic);

    public MagicCasting(ICharAttribDataService svc, IPlayerActionLogService log)
    {
        _svc = svc;
        _log = log;
    }

    public async Task<SubFeatureResult> ApplyCastAsync(CastRequest request)
    {
        var roll = request.Roll;
        if (roll == null || roll.DiceCount < 1) return Fail("There's no roll to apply.");

        string techniqueNames = string.Join(", ", request.Techniques.Select(t => t.Name));
        string what = request.Spell != null
            ? $"Cast spell: {request.Spell.Name}"
            : string.IsNullOrWhiteSpace(request.CombinedName)
                ? $"Sculpted a cantrip using {techniqueNames}"
                : $"Sculpted cantrip \"{request.CombinedName}\" (techniques: {techniqueNames})";

        // ----- the log line the GM reads -----
        string blast = roll.SeriesIsBlast ? ", blast" : string.Empty;
        var parts = new List<string> { $"{roll.DiceCount} MD rolled ({string.Join(", ", roll.Dice)})" };
        parts.Add(request.Spell != null
            ? $"[dice] {roll.DiceCount}, [sum] {roll.Sum}"
            : $"damage {roll.Sum} + {roll.DiceCount} = {roll.CantripDamage}");
        if (roll.TechniqueDice.Count > 0) parts.Add($"technique dice: {string.Join(", ", roll.TechniqueDice)}");
        if (request.Spell != null && request.Techniques.Count > 0) parts.Add($"techniques applied: {techniqueNames}");
        parts.Add(roll.Fatigue > 0 ? $"fatigue +{roll.Fatigue}" : "no fatigue");
        if (roll.HasSeries)
            parts.Add(request.IgnoreDamage
                ? $"SERIES {roll.SeriesText}: {roll.SeriesDamage} damage ignored (GM ruling{blast})"
                : $"SERIES {roll.SeriesText}: {roll.SeriesDamage} damage{blast}");

        await LogAsync(PlayerActionType.SubFeatureUsed, $"{what} — {string.Join("; ", parts)}");

        // ----- consequences -----
        if (roll.Fatigue > 0) _svc.AddFatigue(roll.Fatigue);   // logs its own entry and autosaves

        string damageText = string.Empty;
        if (roll.HasSeries && !request.IgnoreDamage)
        {
            var (hp, mind) = ApplyBacklash(roll.SeriesDamage);
            await LogAsync(PlayerActionType.DamageTaken,
                $"Magic backlash from a series ({roll.SeriesText}): {roll.SeriesDamage} damage — " +
                $"{hp} to Hit Protection, {mind} to Mind{blast}");
            damageText = $" Series backlash: {roll.SeriesDamage} damage ({hp} Hit Protection, {mind} Mind).";
        }
        else if (roll.HasSeries)
        {
            damageText = " Series damage ignored.";
        }

        await _svc.SaveNowAsync();

        string result = request.Spell != null
            ? $"Cast {request.Spell.Name}: [dice] {roll.DiceCount}, [sum] {roll.Sum}."
            : $"Cantrip damage {roll.CantripDamage}.";
        string fatigueText = roll.Fatigue > 0 ? $" Fatigue +{roll.Fatigue}." : string.Empty;
        return new SubFeatureResult { Success = true, Message = result + fatigueText + damageText };
    }

    // Magic backlash: Hit Protection first, the rest goes to MIND (not Vigor). No armor, no Vigor save.
    private (int Hp, int Mind) ApplyBacklash(int amount)
    {
        int hpLost = Math.Min(amount, _svc.CurrentHitpoints);
        _svc.HitpointsDamage += hpLost;

        int overflow = amount - hpLost;
        int mindBefore = _svc.CurrentStatMind;
        if (overflow > 0) _svc.AdjustCurrentStat("mind", -overflow);   // logs the loss and any onset of madness
        return (hpLost, mindBefore - _svc.CurrentStatMind);
    }

    private Task LogAsync(PlayerActionType type, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });

    private static SubFeatureResult Fail(string message) => new() { Success = false, Message = message };
}