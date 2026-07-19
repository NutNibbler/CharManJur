using CharManJur.Models;

namespace CharManJur.Services;

public interface IItemRecoveryService
{
    Task AddRecoverableAsync(CharacterItem item, Guid playerId);
    Task<List<CharacterItem>> GetAllRecoverablesAsync();
    Task<List<CharacterItem>> GetRecoverablesForPlayerAsync(Guid playerId);
    Task<CharacterItem?> RecoverItemAsync(Guid instanceId, int newCharacterItemId);

}