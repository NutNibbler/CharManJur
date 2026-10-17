using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface ITechniqueDataService
{
    Task<List<Technique>> GetTechniquesAsync();
    Task<List<Technique>> GetTechniquesByFeatureIdAsync(int featureId);
    Task<List<Technique>> GetTechniquesByClassIdAsync(int classId);
    Task<List<Technique>> GetTechniquesForCharacterAsync(int classId, int featureId);
    Task<Technique?> GetTechniqueByIdAsync(int id);
    Task<Technique?> GetTechniqueByGuidAsync(Guid guid);
    Task<Technique> CreateCustomTechniqueAsync(CreateCustomTechniqueRequest request);
    Task<bool> UpdateTechniqueAsync(Technique technique);
    Task<bool> DeleteTechniqueAsync(Guid guid);

    // == CUSTOM TECHNIQUE TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Technique>> GetCustomTechniquesAsync();

    // === LOADER METHODS ===
    Task<bool> LoadTechniqueAsync(Guid guid);
    Task<bool> UnloadTechniqueAsync(Guid guid);
}

public class CreateCustomTechniqueRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Populated automatically when created from inside character creation (the
    // popup's ambient class/feature context); left at defaults (0 / null) when
    // created from the Asset Manager, meaning the new Technique won't appear during
    // character creation until a future live-game unlock system provides another
    // path to it. Note UnlockFeatureId is non-nullable, matching Technique.cs.
    public int? UnlockClassId { get; set; }
    public int UnlockFeatureId { get; set; }
}