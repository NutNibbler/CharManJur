using CharManJur.Models;

namespace CharManJur.Services;

public interface IItemRecoveryService
{
    Task AddRecoverableAsync(CharacterItem item, Guid playerId, Guid sessionId);
    Task<List<CharacterItem>> GetAllRecoverablesAsync();
    Task<List<CharacterItem>> GetRecoverablesForPlayerAsync(Guid playerId);
    Task<List<CharacterItem>> GetRecoverablesForSessionAsync(Guid playerId, Guid sessionId);
    Task<CharacterItem?> RecoverItemAsync(Guid instanceId, int newCharacterItemId);

    // Deletes this player's recoverables from every session except keepSessionId (null keeps none).
    Task PurgePlayerRecoverablesAsync(Guid playerId, Guid? keepSessionId);
}