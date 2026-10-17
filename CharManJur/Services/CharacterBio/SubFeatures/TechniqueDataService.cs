using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class TechniqueDataService : ITechniqueDataService
{
    private readonly ICustomTechniqueStorageService _customTechniqueStorage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _customTechniquesLoaded = false;
    private List<Technique> _customTechniques = new();

    private readonly List<Technique> _techniques = new()
    {
        new Technique
        {
            Id = 1,
            TechniqueName = "Range",
            TechniqueDescription = "Invest +1 MD to reach a target far away."
        },
        new Technique
        {
            Id = 2,
            TechniqueName = "Elements",
            TechniqueDescription = "Invest +1 MD to imbue your spell with a particular element, i.e. fire, ice, lightning."
        },
        new Technique
        {
            Id = 3,
            TechniqueName = "Subtlety",
            TechniqueDescription = "Invest +1 MD to cast a silent spell."
        },
        new Technique
        {
            Id = 4,
            TechniqueName = "Force",
            TechniqueDescription = "Invest +1 MD to push your targets back."
        },
        new Technique
        {
            Id = 5,
            TechniqueName = "Pierce",
            TechniqueDescription = "Invest +1 MD to pierce armor. On a 1-3 you bypass 1 armor, on a 4-6 you bypass 2. "
        },
        new Technique
        {
            Id = 6,
            TechniqueName = "Split",
            TechniqueDescription = "Invest +1 MD to split your spell (including effects) to 2 different targets. "
        },
        new Technique
        {
            Id = 7,
            TechniqueName = "Blast",
            TechniqueDescription = "Invest +1 MD to change the properties of the energy to the Blast property. The invested dice determine how many targets are affected within close range of a specific point. "
        },
        new Technique
        {
            Id = 8,
            TechniqueName = "Fast",
            TechniqueDescription = "Invest +1 MD to cast the spell as 1 action instead of 2, allowing the spell to be a quick turn."
        }
    };

    public TechniqueDataService(ICustomTechniqueStorageService customTechniqueStorage)
    {
        _customTechniqueStorage = customTechniqueStorage;
    }

    private async Task EnsureCustomTechniquesLoadedAsync()
    {
        if (_customTechniquesLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_customTechniquesLoaded) return;

            var loaded = await _customTechniqueStorage.LoadCustomTechniquesAsync();
            bool needsResave = false;

            foreach (var technique in loaded)
            {
                // One-time migration: anything still at Guid.Empty (pre-migration saves)
                // gets a real Guid assigned and the file gets re-saved. Same pattern as
                // the other data services' migration pass.
                if (technique.Guid == Guid.Empty)
                {
                    technique.Guid = Guid.NewGuid();
                    needsResave = true;
                }
            }

            _customTechniques = loaded;

            if (needsResave)
            {
                await _customTechniqueStorage.SaveCustomTechniquesAsync(_customTechniques);
            }

            _customTechniquesLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Task<List<Technique>> GetTechniquesAsync()
    {
        return Task.FromResult(_techniques);
    }

    public Task<List<Technique>> GetTechniquesByFeatureIdAsync(int featureId)
    {
        var result = _techniques.Where(t => t.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Technique>> GetTechniquesByClassIdAsync(int classId)
    {
        var result = _techniques.Where(t => t.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public async Task<List<Technique>> GetTechniquesForCharacterAsync(int classId, int featureId)
    {
        await EnsureCustomTechniquesLoadedAsync();

        var combined = new List<Technique>(_techniques);
        combined.AddRange(_customTechniques.Where(t => t.IsLoaded));

        var result = combined.Where(t =>
            t.UnlockClassId == classId ||
            t.UnlockFeatureId == featureId)
            .ToList();

        return result;
    }

    public Task<Technique?> GetTechniqueByIdAsync(int id)
    {
        var foundation = _techniques.FirstOrDefault(t => t.Id == id);
        if (foundation != null) return Task.FromResult<Technique?>(foundation);

        var custom = _customTechniques.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(custom);
    }

    public async Task<Technique?> GetTechniqueByGuidAsync(Guid guid)
    {
        await EnsureCustomTechniquesLoadedAsync();
        return _customTechniques.FirstOrDefault(t => t.Guid == guid);
    }

    public async Task<List<Technique>> GetCustomTechniquesAsync()
    {
        // Deliberately unfiltered — the Asset Manager's own "show everything
        // regardless of state" method, used by the manager UI and AssetPackService.
        await EnsureCustomTechniquesLoadedAsync();
        return _customTechniques;
    }

    public async Task<Technique> CreateCustomTechniqueAsync(CreateCustomTechniqueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Technique name is required.");
        }

        await EnsureCustomTechniquesLoadedAsync();

        var newTechnique = new Technique
        {
            Guid = Guid.NewGuid(),
            Id = AssignNextLoadedCustomTechniqueId(),
            IsLoaded = true,
            SourcePackId = "Local",
            LastModified = DateTime.UtcNow,
            IsPlayerCreated = true,
            TechniqueName = request.Name,
            TechniqueDescription = request.Description,
            UnlockClassId = request.UnlockClassId,
            UnlockFeatureId = request.UnlockFeatureId
        };

        _customTechniques.Add(newTechnique);
        await _customTechniqueStorage.SaveCustomTechniqueAsync(newTechnique);

        return newTechnique;
    }

    public async Task<bool> UpdateTechniqueAsync(Technique technique)
    {
        await EnsureCustomTechniquesLoadedAsync();

        var existing = _customTechniques.FirstOrDefault(t => t.Guid == technique.Guid);
        if (existing == null) return false;

        int index = _customTechniques.IndexOf(existing);
        _customTechniques[index] = technique;

        technique.LastModified = DateTime.UtcNow;
        await _customTechniqueStorage.SaveCustomTechniqueAsync(technique);
        return true;
    }

    public async Task<bool> DeleteTechniqueAsync(Guid guid)
    {
        await EnsureCustomTechniquesLoadedAsync();

        var existing = _customTechniques.FirstOrDefault(t => t.Guid == guid);
        if (existing == null) return false;

        _customTechniques.Remove(existing);
        await _customTechniqueStorage.DeleteCustomTechniqueAsync(guid);
        return true;
    }

    public async Task<bool> LoadTechniqueAsync(Guid guid)
    {
        await EnsureCustomTechniquesLoadedAsync();

        var technique = _customTechniques.FirstOrDefault(t => t.Guid == guid);
        if (technique == null) return false;

        technique.IsLoaded = true;
        technique.Id = AssignNextLoadedCustomTechniqueId();

        await _customTechniqueStorage.SaveCustomTechniquesAsync(_customTechniques);
        return true;
    }

    public async Task<bool> UnloadTechniqueAsync(Guid guid)
    {
        await EnsureCustomTechniquesLoadedAsync();

        var technique = _customTechniques.FirstOrDefault(t => t.Guid == guid);
        if (technique == null) return false;

        technique.IsLoaded = false;
        technique.Id = 0;

        await _customTechniqueStorage.SaveCustomTechniquesAsync(_customTechniques);
        return true;
    }

    // Shared private helper — scans only currently-loaded custom techniques for the
    // max ID, returns +1 (floor 30001). Mirrors the other data services' ID assignment.
    private int AssignNextLoadedCustomTechniqueId()
    {
        var maxId = _customTechniques
            .Where(t => t.IsLoaded)
            .Select(t => t.Id)
            .DefaultIfEmpty(Technique.CUSTOM_TECHNIQUE_BASE)
            .Max();

        return Math.Max(maxId + 1, Technique.CUSTOM_TECHNIQUE_START);
    }
}