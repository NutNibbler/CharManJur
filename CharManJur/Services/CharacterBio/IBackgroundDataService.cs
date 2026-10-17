using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IBackgroundDataService
{
    Task<List<CharacterBackground>> GetBackgroundsAsync();
    Task<CharacterBackground?> GetBackgroundByIdAsync(int id);

    // Includes custom backgrounds, filtered to IsLoaded == true
    Task<List<CharacterBackground>> GetAllBackgroundsAsync();

    // Checks both foundation and custom, filtered to IsLoaded == true
    Task<CharacterBackground?> GetBackgroundByIdCombinedAsync(int id);

    Task<CharacterBackground?> GetBackgroundByGuidAsync(Guid guid);

    Task<CharacterBackground> CreateCustomBackgroundAsync(CreateCustomBackgroundRequest request);

    // === UPDATE METHODS ===
    Task<bool> UpdateBackgroundAsync(CharacterBackground background);

    // === DELETION METHODS ===
    Task<bool> DeleteBackgroundAsync(Guid guid);

    // == CUSTOM BACKGROUND TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<CharacterBackground>> GetCustomBackgroundsAsync();

    // === LOADER METHODS ===
    Task<bool> LoadBackgroundAsync(Guid guid);
    Task<bool> UnloadBackgroundAsync(Guid guid);
}