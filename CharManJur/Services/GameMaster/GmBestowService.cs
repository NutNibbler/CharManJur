using CharManJur.Helpers;
using CharManJur.Models;

namespace CharManJur.Services;

// Gives things to saved characters on the GM's behalf. Until networking exists this is the stand-in for sending
// a "the GM gave you this" message: the character is loaded on its own, changed, and saved back to its file.
public sealed class GmBestowService : IGmBestowService
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
    private readonly IFamiliarDataService _familiars;
    private readonly ICampaignStore _campaigns;

    private LiveSubFeatures? _catalogHelper;

    public GmBestowService(
        IClassDataService classes,
        IPlayerActionLogService log,
        IItemDataService items,
        IItemRecoveryService recovery,
        ICharacterPersistenceService persistence,
        IBlueprintDataService blueprints,
        IQuipDataService quips,
        ISpellDataService spells,
        ITechniqueDataService techniques,
        IFamiliarDataService familiars,
        ICampaignStore campaigns)
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
        _familiars = familiars;
        _campaigns = campaigns;
    }

    // ===== WHAT CAN BE GIVEN =====
    public async Task<List<Item>> GetItemsAsync() => (await _items.GetAllItemsAsync()).ToList();

    public async Task<List<Familiar>> GetFamiliarsAsync() => (await _familiars.GetAllFamiliarsAsync()).ToList();
    public async Task<List<ScratchPageInfo>> GetPadPagesAsync(Guid campaignId)
    {
        var index = await new ScratchPadStore(_campaigns.PadFolder(campaignId)).LoadIndexAsync();
        return index.Pages.ToList();
    }

    // The live game already knows how to list everything that exists for a type, so its catalog is reused.
    public Task<List<SubFeatureEntry>> GetSubFeaturesAsync(SubFeatureType type)
    {
        _catalogHelper ??= new LiveSubFeatures(NewHeadless(), _blueprints, _quips, _spells, _techniques, _log);
        return _catalogHelper.CatalogAsync(type);
    }

    // A character service that belongs to no screen: it is filled from a save file, changed, and saved.
    private CharAttribDataService NewHeadless() => new(_classes, _log, _items, _recovery, _persistence);

    // ===== BESTOWING =====
    public async Task<List<GmBestowOutcome>> BestowAsync(
        IReadOnlyList<CharacterSaveData> recipients, IReadOnlyList<GmBestowLine> lines, Guid? sessionId)
    {
        var outcomes = new List<GmBestowOutcome>();
        foreach (var recipient in recipients)
            outcomes.Add(await BestowToOneAsync(recipient, lines, sessionId));
        return outcomes;
    }

    private async Task<GmBestowOutcome> BestowToOneAsync(
        CharacterSaveData recipient, IReadOnlyList<GmBestowLine> lines, Guid? sessionId)
    {
        var outcome = new GmBestowOutcome { Recipient = $"{recipient.CharacterName} ({recipient.PlayerName})" };

        try
        {
            // Always start from what is on disk right now.
            var save = await _persistence.LoadCharacterAsync(recipient.FileName);
            if (save == null)
            {
                outcome.Error = "their save file wasn't found.";
                return outcome;
            }

            var svc = NewHeadless();
            await svc.PopulateFromSaveDataAsync(save, startSession: false);   // no session, no recoverables purge

            var live = new LiveSubFeatures(svc, _blueprints, _quips, _spells, _techniques, _log);

            using (ActionLogScope.Begin(ActionSource.GM, sessionId))
            {
                foreach (var line in lines)
                {
                    try
                    {
                        await ApplyLineAsync(svc, live, line, outcome);
                    }
                    catch (Exception ex)
                    {
                        outcome.Notes.Add($"couldn't give {line.Name} ({ex.Message})");
                    }
                }

                // Heavy loot can tip a character into encumbrance; the rule applies, and the GM is told.
                svc.UpdateEncumbranceState();
                if (svc.IsEncumbered) outcome.Notes.Add("now encumbered");

                if (!await SaveSettledAsync(svc))
                    outcome.Error = "the changes couldn't be confirmed as saved. Check the character before trying again.";
            }
        }
        catch (Exception ex)
        {
            outcome.Error = ex.Message;
        }

        return outcome;
    }

    private async Task ApplyLineAsync(ICharAttribDataService svc, LiveSubFeatures live, GmBestowLine line, GmBestowOutcome outcome)
    {
        switch (line.Kind)
        {
            case GmBestowKind.Item:
                await GiveItemAsync(svc, line, outcome);
                break;

            case GmBestowKind.Familiar:
                {
                    var instance = svc.BestowFamiliar(line.Familiar!);   // logs itself; always a fresh copy
                    outcome.Given.Add(instance.DisplayName);
                    break;
                }
            case GmBestowKind.ScratchPage:
                await GivePageAsync(svc, line, outcome);
                break;

            default:
                {
                    var entry = line.SubFeature!;
                    string kind = LiveSubFeatures.TypeName(entry.Type).ToLowerInvariant();

                    if (live.IsOwned(entry))
                    {
                        outcome.Notes.Add($"already knows the {kind} {entry.Name}");
                        break;
                    }

                    var result = await live.BestowAsync(entry);   // logs and saves
                    if (result.Success) outcome.Given.Add($"{kind} {entry.Name}");
                    else outcome.Notes.Add($"couldn't give the {kind} {entry.Name}: {result.Message}");
                    break;
                }
        }
    }

    // A page from the GM's pad becomes a new page in the player's own pad (a copy: later edits to either don't affect the other).
    private async Task GivePageAsync(ICharAttribDataService svc, GmBestowLine line, GmBestowOutcome outcome)
    {
        var page = line.Page!;
        var from = new ScratchPadStore(_campaigns.PadFolder(line.CampaignId));
        var to = new ScratchPadStore(svc.CharacterGuid);

        var copied = await ScratchPadStore.CopyPageAsync(from, to, page.Id, $"{page.Name} (from GM)");
        if (copied == null)
        {
            outcome.Notes.Add($"couldn't give the page {page.Name}");
            return;
        }

        await _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ScratchPageBestowed,
            PlayerName = svc.PlayerName,
            PlayerId = svc.PlayerId,
            CharacterName = svc.CharacterName,
            CharacterGuid = svc.CharacterGuid,
            Summary = $"The GM gave a scratch pad page: {page.Name}"
        });

        outcome.Given.Add($"page {page.Name}");
    }

    // Puts an item line into a character's pack. The same rules the real bestow and the preview both use:
    // a stackable item goes in as one stack (which splits at its limit), anything else is one record per item.
    private static CharacterItem? AddItemRecords(ICharAttribDataService svc, GmBestowLine line)
    {
        var item = line.Item!;
        int quantity = Math.Max(1, line.Quantity);

        if (item.IsStackableItem)
            return svc.AddItemToInventory(item, quantity, null, null, line.UsageDieSides);

        CharacterItem? last = null;
        for (int i = 0; i < quantity; i++)
            last = svc.AddItemToInventory(item, 1, line.WrittenValue, null, line.UsageDieSides);
        return last;
    }

    public async Task<List<string>> PreviewEncumbranceAsync(
        IReadOnlyList<CharacterSaveData> recipients, IReadOnlyList<GmBestowLine> lines)
    {
        var warnings = new List<string>();
        var itemLines = lines.Where(l => l.Kind == GmBestowKind.Item && l.Item != null).ToList();
        if (itemLines.Count == 0) return warnings;   // only items take pack slots

        foreach (var recipient in recipients)
        {
            try
            {
                var save = await _persistence.LoadCharacterAsync(recipient.FileName);
                if (save == null) continue;

                // A copy that is never saved. AddItemToInventory logs nothing and doesn't autosave,
                // and UpdateEncumbranceState (which would) is deliberately not called here.
                var svc = NewHeadless();
                await svc.PopulateFromSaveDataAsync(save, startSession: false);

                bool alreadyEncumbered = svc.IsEncumbered;
                foreach (var line in itemLines) AddItemRecords(svc, line);

                if (!alreadyEncumbered && svc.IsEncumbered)
                    warnings.Add($"{recipient.CharacterName} ({recipient.PlayerName}) would carry {svc.InventorySlotsUsed} pack slots " +
                                 $"with room for {svc.InventorySlotsAvailable}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encumbrance preview error for {recipient.CharacterName}: {ex.Message}");
            }
        }

        return warnings;
    }

    // Same rules as the player's bestow page: a stackable item goes in as one stack (which splits at its limit),
    // anything else is one record per item. Starting uses stay full.
    private async Task GiveItemAsync(ICharAttribDataService svc, GmBestowLine line, GmBestowOutcome outcome)
    {
        var item = line.Item!;
        int quantity = Math.Max(1, line.Quantity);

        var last = AddItemRecords(svc, line);

        if (last == null)
        {
            outcome.Notes.Add($"couldn't give {item.Name}");
            return;
        }

        string die = line.UsageDieSides is { } sides ? $" with a {UsageDie.Label(sides)} usage die" : string.Empty;
        await _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemBestowed,
            PlayerName = svc.PlayerName,
            PlayerId = svc.PlayerId,
            CharacterName = svc.CharacterName,
            CharacterGuid = svc.CharacterGuid,
            ItemName = item.Name,
            ItemId = item.Id,
            Summary = quantity > 1
                ? $"The GM bestowed {quantity}x {item.Name}{die}"
                : $"The GM bestowed {item.Name}{die}"
        });

        outcome.Given.Add(quantity > 1 ? $"{quantity}× {item.Name}" : item.Name);
    }

    // Some character actions autosave without waiting (a familiar, a change in encumbrance), and two writes to
    // one file at the same moment can lose one of them. So wait for those to finish, write the final state, then
    // read the file back to confirm it holds everything. Tries up to three times.
    private async Task<bool> SaveSettledAsync(ICharAttribDataService svc)
    {
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
                && d.AcquiredBlueprints.Count == svc.AcquiredBlueprints.Count)
                return true;
        }
        return false;
    }
}