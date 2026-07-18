using CharManJur.Models;

namespace CharManJur.Services;

public interface ISubFeatureDataService
{
    // === BLUEPRINTS ===
    Task<List<Blueprint>> GetBlueprintsAsync();
    Task<List<Blueprint>> GetBlueprintsByFeatureIdAsync(int featureId);
    Task<Blueprint?> GetBlueprintByIdAsync(int id);

    // === QUIPS ===
    Task<List<Quip>> GetQuipsAsync();
    Task<List<Quip>> GetQuipsByFeatureIdAsync(int featureId);
    Task<Quip?> GetQuipByIdAsync(int id);

    // === SPELLS ===
    Task<List<Spell>> GetSpellsAsync();
    Task<List<Spell>> GetSpellsByFeatureIdAsync(int featureId);
    Task<Spell?> GetSpellByIdAsync(int id);

    // === TECHNIQUES ===
    Task<List<Technique>> GetTechniquesAsync();
    Task<List<Technique>> GetTechniquesByFeatureIdAsync(int featureId);
    Task<Technique?> GetTechniqueByIdAsync(int id);
}