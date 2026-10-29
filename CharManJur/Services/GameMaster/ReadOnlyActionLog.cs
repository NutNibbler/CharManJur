using CharManJur.Models;

namespace CharManJur.Services;

// The action log as seen by the GM's Player Viewer: reading works (the Event Tray shows the player's real
// history), but nothing done in the viewer is recorded.
public sealed class ReadOnlyActionLog : IPlayerActionLogService
{
    private readonly IPlayerActionLogService _inner;

    public ReadOnlyActionLog(IPlayerActionLogService inner) => _inner = inner;

    // Ignored: the viewer must never change the shared log's current session.
    public Guid? CurrentSessionId { get; set; }

    public event Action<PlayerActionLogEntry>? EntryLogged
    {
        add => _inner.EntryLogged += value;
        remove => _inner.EntryLogged -= value;
    }

    public Task LogEventAsync(PlayerActionLogEntry entry) => Task.CompletedTask;

    public Task<List<PlayerActionLogEntry>> GetAllEntriesAsync() => _inner.GetAllEntriesAsync();

    public Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Task.CompletedTask;

    public Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Task.CompletedTask;

    public Task LogItemRecoveredAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid)
        => Task.CompletedTask;
}