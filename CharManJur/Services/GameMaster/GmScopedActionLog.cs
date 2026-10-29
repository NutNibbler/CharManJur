using CharManJur.Models;

namespace CharManJur.Services;

// A view of the action log for a character the GM is changing. Everything logged through it is marked as the
// GM's and stamped with the session, so the GM's tools can reuse the player's own screens. It keeps its own
// session id, so a character service using it can never change the shared log's current session.
public sealed class GmScopedActionLog : IPlayerActionLogService
{
    private readonly IPlayerActionLogService _inner;
    private readonly Guid? _sessionId;

    public GmScopedActionLog(IPlayerActionLogService inner, Guid? sessionId)
    {
        _inner = inner;
        _sessionId = sessionId;
    }

    public Guid? CurrentSessionId { get; set; }

    public event Action<PlayerActionLogEntry>? EntryLogged
    {
        add => _inner.EntryLogged += value;
        remove => _inner.EntryLogged -= value;
    }

    public Task LogEventAsync(PlayerActionLogEntry entry)
    {
        entry.Source = ActionSource.GM;
        entry.SessionId = _sessionId ?? entry.SessionId;
        return _inner.LogEventAsync(entry);
    }

    public Task<List<PlayerActionLogEntry>> GetAllEntriesAsync() => _inner.GetAllEntriesAsync();

    public Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemDropped,
            PlayerName = playerName,
            PlayerId = playerId,
            CharacterName = characterName,
            CharacterGuid = characterGuid,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId,
            Summary = item.Quantity > 1
                ? $"Dropped {item.Quantity}x {item.DisplayName}"
                : $"Dropped {item.DisplayName}"
        });

    public Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemBestowed,
            PlayerName = playerName,
            PlayerId = playerId,
            CharacterName = characterName,
            CharacterGuid = characterGuid,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId,
            Summary = $"Bestowed {item.DisplayName}"
        });

    public Task LogItemRecoveredAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemRecovered,
            PlayerName = playerName,
            PlayerId = playerId,
            CharacterName = characterName,
            CharacterGuid = characterGuid,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId,
            Summary = $"Recovered {item.DisplayName}"
        });
}