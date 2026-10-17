using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface IBlueprintDataService
{
    Task<List<Blueprint>> GetBlueprintsAsync();
    Task<List<Blueprint>> GetBlueprintsByFeatureIdAsync(int featureId);
    Task<List<Blueprint>> GetBlueprintsByClassIdAsync(int classId);
    Task<List<Blueprint>> GetBlueprintsForCharacterAsync(int classId, int featureId);
    Task<Blueprint?> GetBlueprintByIdAsync(int id);
    Task<Blueprint?> GetBlueprintByGuidAsync(Guid guid);
    Task<Blueprint> CreateCustomBlueprintAsync(CreateCustomBlueprintRequest request);
    Task<bool> UpdateBlueprintAsync(Blueprint blueprint);
    Task<bool> DeleteBlueprintAsync(Guid guid);

    // == CUSTOM BLUEPRINT TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Blueprint>> GetCustomBlueprintsAsync();

    // === LOADER METHODS ===
    Task<bool> LoadBlueprintAsync(Guid guid);
    Task<bool> UnloadBlueprintAsync(Guid guid);
}

public class CreateCustomBlueprintRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }

    // Populated automatically when created from inside character creation (the
    // popup's ambient class/feature context); left null when created from the
    // Asset Manager, which makes the new Blueprint universally visible.
    public int? UnlockClassId { get; set; }
    public int? UnlockFeatureId { get; set; }
}