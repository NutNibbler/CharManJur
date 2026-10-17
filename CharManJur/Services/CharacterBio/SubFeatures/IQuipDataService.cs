using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface IQuipDataService
{
    Task<List<Quip>> GetQuipsAsync();
    Task<List<Quip>> GetQuipsByFeatureIdAsync(int featureId);
    Task<List<Quip>> GetQuipsByClassIdAsync(int classId);
    Task<List<Quip>> GetQuipsForCharacterAsync(int classId, int featureId);
    Task<Quip?> GetQuipByIdAsync(int id);
    Task<Quip?> GetQuipByGuidAsync(Guid guid);
    Task<Quip> CreateCustomQuipAsync(CreateCustomQuipRequest request);
    Task<bool> UpdateQuipAsync(Quip quip);
    Task<bool> DeleteQuipAsync(Guid guid);

    // == CUSTOM QUIP TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Quip>> GetCustomQuipsAsync();

    // === LOADER METHODS ===
    Task<bool> LoadQuipAsync(Guid guid);
    Task<bool> UnloadQuipAsync(Guid guid);
}

public class CreateCustomQuipRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Populated automatically when created from inside character creation (the
    // popup's ambient class/feature context); left null when created from the
    // Asset Manager, meaning the new Quip won't appear during character creation
    // until a future live-game unlock system provides another path to it.
    public int? UnlockClassId { get; set; }
    public int? UnlockFeatureId { get; set; }
}