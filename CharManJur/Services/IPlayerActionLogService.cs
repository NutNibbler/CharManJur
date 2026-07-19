using CharManJur.Models;

namespace CharManJur.Services;

public interface IPlayerActionLogService
{
    Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId);
    Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId);
    Task<List<PlayerActionLogEntry>> GetAllEntriesAsync();
}