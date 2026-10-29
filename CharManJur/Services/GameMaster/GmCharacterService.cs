using CharManJur.Models;

namespace CharManJur.Services;

public sealed class GmCharacterService : IGmCharacterService
{
    private readonly IClassDataService _classes;
    private readonly IPlayerActionLogService _log;
    private readonly IItemDataService _items;
    private readonly IItemRecoveryService _recovery;
    private readonly ICharacterPersistenceService _persistence;
    private readonly IBlueprintDataService _blueprints;
    private readonly IQuipDataService _quips;
    private readonly ISpellDataService _spells;
    private readonly ITechniqueDataService _techniques;

    public GmCharacterService(
        IClassDataService classes,
        IPlayerActionLogService log,
        IItemDataService items,
        IItemRecoveryService recovery,
        ICharacterPersistenceService persistence,
        IBlueprintDataService blueprints,
        IQuipDataService quips,
        ISpellDataService spells,
        ITechniqueDataService techniques)
    {
        _classes = classes;
        _log = log;
        _items = items;
        _recovery = recovery;
        _persistence = persistence;
        _blueprints = blueprints;
        _quips = quips;
        _spells = spells;
        _techniques = techniques;
    }

    public async Task<GmCharacterEdit?> OpenAsync(CharacterSaveData recipient, Guid? sessionId)
    {
        // Always start from what is on disk right now.
        var save = await _persistence.LoadCharacterAsync(recipient.FileName);
        if (save == null) return null;

        var log = new GmScopedActionLog(_log, sessionId);
        var svc = new CharAttribDataService(_classes, log, _items, _recovery, _persistence);
        await svc.PopulateFromSaveDataAsync(save, startSession: false);   // no session, no recoverables purge

        return new GmCharacterEdit { Recipient = recipient, Svc = svc, Log = log };
    }

    public async Task<GmViewerSource?> OpenViewerAsync(CharacterSaveData recipient)
    {
        var save = await _persistence.LoadCharacterAsync(recipient.FileName);
        if (save == null) return null;

        var log = new ReadOnlyActionLog(_log);
        var persistence = new ReadOnlyPersistence(_persistence);

        var svc = new CharAttribDataService(_classes, log, _items, _recovery, persistence) { IsReadOnly = true };
        await svc.PopulateFromSaveDataAsync(save, startSession: false);   // no session, no recoverables purge

        return new GmViewerSource { Svc = svc, Log = log, Persistence = persistence };
    }

    public LiveSubFeatures NewSubFeatures(GmCharacterEdit edit)
        => new(edit.Svc, _blueprints, _quips, _spells, _techniques, edit.Log);

    public async Task<GmPlayerSummary> GetSummaryAsync(CharacterSaveData recipient)
    {
        try
        {
            var save = await _persistence.LoadCharacterAsync(recipient.FileName) ?? recipient;

            var svc = new CharAttribDataService(_classes, _log, _items, _recovery, _persistence) { IsReadOnly = true };
            await svc.PopulateFromSaveDataAsync(save, startSession: false);

            // An encumbered character's Max HP is 0 (the live game applies that when its screen loads).
            bool encumbered = svc.IsEncumbered;
            int maxHp = encumbered ? 0 : svc.Hitpoints;
            int hp = encumbered ? 0 : svc.CurrentHitpoints;

            int fatigue = svc.FatigueTotal, fatigueMax = svc.FatigueBarMax, overflow = svc.FatigueInInventory;
            int used = svc.InventorySlotsUsed, room = svc.InventorySlotsAvailable;

            // 0 = full, 1 = below its max, 2 = gone.
            static int Level(int current, int max) => current <= 0 ? 2 : current < max ? 1 : 0;

            // While transformed, damage hits Stability and the form drops before HP is touched, so Stability replaces HP.
            bool transformed = svc.Transform is { IsTransformed: true };
            int stability = transformed ? (int)svc.Transform!.StabilityCurrent : 0;
            int stabilityMax = transformed ? (int)svc.Transform!.StabilityMax : 0;

            var firstChip = transformed
                ? new GmStatChip("🐺 STAB", $"{stability}/{stabilityMax}", Level(stability, stabilityMax))
                : new GmStatChip("HP", $"{hp}/{maxHp}", Level(hp, maxHp));

            var chips = new List<GmStatChip>
            {
                firstChip,
                new GmStatChip("VIG", $"{svc.CurrentStatVigor}/{svc.TotalStatVigor}", Level(svc.CurrentStatVigor, svc.TotalStatVigor)),
                new GmStatChip("AGI", $"{svc.CurrentStatAgility}/{svc.TotalStatAgility}", Level(svc.CurrentStatAgility, svc.TotalStatAgility)),
                new GmStatChip("MND", $"{svc.CurrentStatMind}/{svc.TotalStatMind}", Level(svc.CurrentStatMind, svc.TotalStatMind)),
                new GmStatChip("SPI", $"{svc.CurrentStatSpirit}/{svc.TotalStatSpirit}", Level(svc.CurrentStatSpirit, svc.TotalStatSpirit)),
                new GmStatChip("Fatigue",
                    overflow > 0 ? $"{fatigue}/{fatigueMax} (+{overflow} in pack)" : $"{fatigue}/{fatigueMax}",
                    overflow > 0 ? 2 : fatigue > 0 ? 1 : 0),
                new GmStatChip("Pack",
                    encumbered ? $"{used}/{room} ENCUMBERED" : $"{used}/{room}",
                    encumbered ? 2 : 0)
            };

            int waiting = svc.PendingDamage.Count + svc.PendingEffects.Count;
            string compact = transformed
                ? $"🐺 STAB {stability}/{stabilityMax} · VIG {svc.CurrentStatVigor}/{svc.TotalStatVigor}"
                : $"HP {hp}/{maxHp} · VIG {svc.CurrentStatVigor}/{svc.TotalStatVigor}";
            if (waiting > 0) compact += $" · 📨 {waiting} waiting";

            return new GmPlayerSummary { Chips = chips, Compact = compact, Waiting = waiting };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GM summary error for {recipient.CharacterName}: {ex.Message}");
            return new GmPlayerSummary();
        }
    }

    // Some character actions autosave without waiting, and two writes to one file at the same moment can lose
    // one of them. So wait for those to finish, write the final state, then read the file back to confirm it
    // holds everything. Tries up to three times.
    public async Task<bool> SaveAsync(GmCharacterEdit edit)
    {
        var svc = edit.Svc;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            await Task.Delay(attempt == 0 ? 300 : 600);
            await svc.SaveNowAsync();

            var check = await _persistence.LoadCharacterAsync(svc.FileName);
            var d = check?.Data;
            if (d != null
                && d.Inventory.Count == svc.Inventory.Count
                && d.AcquiredFamiliars.Count == svc.AcquiredFamiliars.Count
                && d.AcquiredSpells.Count == svc.AcquiredSpells.Count
                && d.AcquiredQuips.Count == svc.AcquiredQuips.Count
                && d.AcquiredTechniques.Count == svc.AcquiredTechniques.Count
                && d.AcquiredBlueprints.Count == svc.AcquiredBlueprints.Count
                && d.ActiveEffects.Count == svc.ActiveEffects.Count
                && d.PendingDamage.Count == svc.PendingDamage.Count
                && d.PendingEffects.Count == svc.PendingEffects.Count
                && d.HitpointsDamage == svc.HitpointsDamage
                && d.FatigueCount == svc.FatigueCount
                && d.CurrentVigorAdjustment == svc.CurrentVigorAdjustment
                && d.CurrentAgilityAdjustment == svc.CurrentAgilityAdjustment
                && d.CurrentMindAdjustment == svc.CurrentMindAdjustment
                && d.CurrentSpiritAdjustment == svc.CurrentSpiritAdjustment)
                return true;
        }
        return false;
    }
}