using CharManJur.Models;

namespace CharManJur.Services;

public interface IPlayerActionLogService
{
    Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid);
    Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid);
    Task LogItemRecoveredAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid);

    // General-purpose entry point for non-item events (damage, rest, fatigue, ability loss, etc).
    Task LogEventAsync(PlayerActionLogEntry entry);

    Task<List<PlayerActionLogEntry>> GetAllEntriesAsync();
    Guid? CurrentSessionId { get; set; }
    // Raised after an entry has been written. May arrive on a background thread.
    event Action<PlayerActionLogEntry>? EntryLogged;
}