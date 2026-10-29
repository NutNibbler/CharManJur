using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

// One thing the GM's bestow page did to the player's copy. The player's app repeats it on the real character.
public sealed class RemoteBestowOp
{
    public string Op { get; set; } = string.Empty;           // "item" or "familiar"
    public string Label { get; set; } = string.Empty;        // for messages, e.g. "2× Torch"
    public int ItemId { get; set; }
    public Guid ItemGuid { get; set; }                       // set for player-created items; empty for foundation items
    public int Quantity { get; set; } = 1;
    public int? WrittenValue { get; set; }
    public int? StartingUses { get; set; }
    public int? UsageDie { get; set; }
    public Guid FamiliarInstanceId { get; set; }             // the item goes to this familiar; empty = the character
    public string? FamiliarJson { get; set; }                // for "familiar": the template being bestowed
}

// The GM's working copy of a network player's character. The bestow pages run against Svc; it records what they do
// (Ops), and Log holds their log lines until the player's app has confirmed the delivery.
public sealed class RemoteBestowSession
{
    public RecordingCharService Svc { get; init; } = null!;
    public BufferedActionLog Log { get; init; } = null!;

    public List<RemoteBestowOp> Ops => Svc.Ops;
    public string Summary => string.Join(", ", Ops.Select(o => o.Label));
    public string Json => JsonSerializer.Serialize(Ops);
}

// A character service that notes the bestow calls made on it. The copy is never saved anywhere.
public sealed class RecordingCharService : CharAttribDataService
{
    public RecordingCharService(IClassDataService classes, IPlayerActionLogService log, IItemDataService items,
        IItemRecoveryService recovery, ICharacterPersistenceService persistence)
        : base(classes, log, items, recovery, persistence)
    {
    }

    public List<RemoteBestowOp> Ops { get; } = new();

    public override CharacterItem AddItemToInventory(Item template, int quantity = 1, int? writtenValueInChips = null,
        int? startingUses = null, int? startingUsageDie = null)
    {
        var result = base.AddItemToInventory(template, quantity, writtenValueInChips, startingUses, startingUsageDie);
        if (result != null)
            Ops.Add(ItemOp(template, quantity, writtenValueInChips, startingUses, startingUsageDie, Guid.Empty, $"{Math.Max(1, quantity)}× {template.Name}"));
        return result!;
    }

    public override bool AddItemToFamiliarInventory(Guid familiarInstanceId, Item template, int quantity = 1,
        int? writtenValueInChips = null, int? startingUses = null, int? startingUsageDie = null)
    {
        bool added = base.AddItemToFamiliarInventory(familiarInstanceId, template, quantity, writtenValueInChips, startingUses, startingUsageDie);
        if (added)
        {
            string who = GetFamiliar(familiarInstanceId)?.DisplayName ?? "a familiar";
            Ops.Add(ItemOp(template, quantity, writtenValueInChips, startingUses, startingUsageDie, familiarInstanceId,
                $"{Math.Max(1, quantity)}× {template.Name} → {who}"));
        }
        return added;
    }

    public override Familiar BestowFamiliar(Familiar template)
    {
        var instance = base.BestowFamiliar(template);
        Ops.Add(new RemoteBestowOp
        {
            Op = "familiar",
            Label = $"familiar {instance.DisplayName}",
            FamiliarJson = JsonSerializer.Serialize(template)
        });
        return instance;
    }

    private static RemoteBestowOp ItemOp(Item template, int quantity, int? writtenValue, int? startingUses, int? usageDie,
        Guid familiarInstanceId, string label) => new()
        {
            Op = "item",
            Label = label,
            ItemId = template.Id,
            ItemGuid = template.IsPlayerCreated ? template.Guid : Guid.Empty,
            Quantity = Math.Max(1, quantity),
            WrittenValue = writtenValue,
            StartingUses = startingUses,
            UsageDie = usageDie,
            FamiliarInstanceId = familiarInstanceId
        };
}

// Holds log lines until the delivery is confirmed (Flush), or drops them if it never was (Discard), so the GM's
// log never claims something was bestowed that the player never received.
public sealed class BufferedActionLog : IPlayerActionLogService
{
    private readonly IPlayerActionLogService _inner;
    private readonly List<Func<Task>> _held = new();

    public BufferedActionLog(IPlayerActionLogService inner) => _inner = inner;

    public Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Hold(() => _inner.LogItemDroppedAsync(item, playerName, playerId, characterName, characterGuid));

    public Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Hold(() => _inner.LogItemBestowedAsync(item, playerName, playerId, characterName, characterGuid));

    public Task LogItemRecoveredAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Hold(() => _inner.LogItemRecoveredAsync(item, playerName, playerId, characterName, characterGuid));

    public Task LogEventAsync(PlayerActionLogEntry entry) => Hold(() => _inner.LogEventAsync(entry));

    public Task<List<PlayerActionLogEntry>> GetAllEntriesAsync() => _inner.GetAllEntriesAsync();

    public Guid? CurrentSessionId
    {
        get => _inner.CurrentSessionId;
        set => _inner.CurrentSessionId = value;
    }

    public event Action<PlayerActionLogEntry>? EntryLogged
    {
        add => _inner.EntryLogged += value;
        remove => _inner.EntryLogged -= value;
    }

    private Task Hold(Func<Task> write)
    {
        lock (_held) _held.Add(write);
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        List<Func<Task>> writes;
        lock (_held)
        {
            writes = _held.ToList();
            _held.Clear();
        }

        foreach (var write in writes)
        {
            try { await write(); }
            catch (Exception ex) { CrashLog.Write("GM: writing a held log line", ex); }
        }
    }

    public void Discard()
    {
        lock (_held) _held.Clear();
    }
}

public interface IRemoteBestowService
{
    // Builds the working copy from the player's fresh copy of their character.
    Task<RemoteBestowSession> OpenAsync(CharacterSaveData copy, Guid? sessionId);
}

public sealed class RemoteBestowService : IRemoteBestowService
{
    private readonly IClassDataService _classes;
    private readonly IPlayerActionLogService _log;
    private readonly IItemDataService _items;
    private readonly IItemRecoveryService _recovery;
    private readonly ICharacterPersistenceService _persistence;

    public RemoteBestowService(IClassDataService classes, IPlayerActionLogService log, IItemDataService items,
        IItemRecoveryService recovery, ICharacterPersistenceService persistence)
    {
        _classes = classes;
        _log = log;
        _items = items;
        _recovery = recovery;
        _persistence = persistence;
    }

    public async Task<RemoteBestowSession> OpenAsync(CharacterSaveData copy, Guid? sessionId)
    {
        var buffered = new BufferedActionLog(new GmScopedActionLog(_log, sessionId));

        // Saves go nowhere: this is only a copy of someone else's character, not a file of the GM's.
        var svc = new RecordingCharService(_classes, buffered, _items, _recovery, new ReadOnlyPersistence(_persistence));
        await svc.PopulateFromSaveDataAsync(copy, startSession: false);   // no session, no recoverables purge

        return new RemoteBestowSession { Svc = svc, Log = buffered };
    }
}