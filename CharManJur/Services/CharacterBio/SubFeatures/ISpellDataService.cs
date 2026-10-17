using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface ISpellDataService
{
    Task<List<Spell>> GetSpellsAsync();
    Task<List<Spell>> GetSpellsByFeatureIdAsync(int featureId);
    Task<List<Spell>> GetSpellsByClassIdAsync(int classId);
    Task<List<Spell>> GetSpellsForCharacterAsync(int classId, int featureId);
    Task<Spell?> GetSpellByIdAsync(int id);
    Task<Spell?> GetSpellByGuidAsync(Guid guid);
    Task<Spell> CreateCustomSpellAsync(CreateCustomSpellRequest request);
    Task<bool> UpdateSpellAsync(Spell spell);
    Task<bool> DeleteSpellAsync(Guid guid);

    // == CUSTOM SPELL TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Spell>> GetCustomSpellsAsync();

    // === LOADER METHODS ===
    Task<bool> LoadSpellAsync(Guid guid);
    Task<bool> UnloadSpellAsync(Guid guid);
}

public class CreateCustomSpellRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dice { get; set; }
    public string? Range { get; set; }

    // Populated automatically when created from inside character creation (the
    // popup's ambient class/feature context); left null when created from the
    // Asset Manager, meaning the new Spell won't appear during character creation
    // until a future live-game unlock system provides another path to it.
    public int? UnlockClassId { get; set; }
    public int? UnlockFeatureId { get; set; }
}