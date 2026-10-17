using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class BlueprintDataService : IBlueprintDataService
{
    private readonly ICustomBlueprintStorageService _customBlueprintStorage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _customBlueprintsLoaded = false;
    private List<Blueprint> _customBlueprints = new();

    private readonly List<Blueprint> _blueprints = new()
    {
        new Blueprint
        {
            Id = 1,
            BlueprintName = "Tool",
            BlueprintDescription = "Held. Things like swords, shovels, drills, etc. ",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 2,
            BlueprintName = "Garment",
            BlueprintDescription = "Worn, gives passive bonuses. Armor, cloaks, etc.",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 3,
            BlueprintName = "Vessel",
            BlueprintDescription = "A container of something. Potions, munitions. Single use.",
            BlueprintCost = 2
        },
        new Blueprint
        {
            Id = 4,
            BlueprintName = "Gauge",
            BlueprintDescription = "Measures something. Hourglass, telescope, etc. ",
            BlueprintCost = 4
        },
        new Blueprint
        {
            Id = 5,
            BlueprintName = "Launcher",
            BlueprintDescription = "Fires something at range. Gun, catapolt, etc. ",
            BlueprintCost = 5
        },
        new Blueprint
        {
            Id = 6,
            BlueprintName = "Apparatus",
            BlueprintDescription = "Exists in place. Can be a work bench, shelter, printer, etc.",
            BlueprintCost = 5
        },
        new Blueprint
        {
            Id = 7,
            BlueprintName = "Transport",
            BlueprintDescription = "Moves under its own power, directly controlled. ",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 8,
            BlueprintName = "Prosthesis",
            BlueprintDescription = "Fused with a living creature taking the place of an organ/extremity. ",
            BlueprintCost = 3
        }
    };

    public BlueprintDataService(ICustomBlueprintStorageService customBlueprintStorage)
    {
        _customBlueprintStorage = customBlueprintStorage;
    }

    private async Task EnsureCustomBlueprintsLoadedAsync()
    {
        if (_customBlueprintsLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_customBlueprintsLoaded) return;

            var loaded = await _customBlueprintStorage.LoadCustomBlueprintsAsync();
            bool needsResave = false;

            foreach (var blueprint in loaded)
            {
                // One-time migration: anything still at Guid.Empty (pre-migration saves)
                // gets a real Guid assigned and the file gets re-saved. Same pattern as
                // the other data services' migration pass.
                if (blueprint.Guid == Guid.Empty)
                {
                    blueprint.Guid = Guid.NewGuid();
                    needsResave = true;
                }
            }

            _customBlueprints = loaded;

            if (needsResave)
            {
                await _customBlueprintStorage.SaveCustomBlueprintsAsync(_customBlueprints);
            }

            _customBlueprintsLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Task<List<Blueprint>> GetBlueprintsAsync()
    {
        return Task.FromResult(_blueprints);
    }

    public Task<List<Blueprint>> GetBlueprintsByFeatureIdAsync(int featureId)
    {
        var result = _blueprints.Where(b => b.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Blueprint>> GetBlueprintsByClassIdAsync(int classId)
    {
        var result = _blueprints.Where(b => b.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public async Task<List<Blueprint>> GetBlueprintsForCharacterAsync(int classId, int featureId)
    {
        await EnsureCustomBlueprintsLoadedAsync();

        var combined = new List<Blueprint>(_blueprints);
        combined.AddRange(_customBlueprints.Where(b => b.IsLoaded));

        var result = combined.Where(b =>
            b.UnlockClassId == classId ||
            b.UnlockFeatureId == featureId)
            .ToList();

        return result;
    }

    public Task<Blueprint?> GetBlueprintByIdAsync(int id)
    {
        var foundation = _blueprints.FirstOrDefault(b => b.Id == id);
        if (foundation != null) return Task.FromResult<Blueprint?>(foundation);

        var custom = _customBlueprints.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(custom);
    }

    public async Task<Blueprint?> GetBlueprintByGuidAsync(Guid guid)
    {
        await EnsureCustomBlueprintsLoadedAsync();
        return _customBlueprints.FirstOrDefault(b => b.Guid == guid);
    }

    public async Task<List<Blueprint>> GetCustomBlueprintsAsync()
    {
        // Deliberately unfiltered — the Asset Manager's own "show everything
        // regardless of state" method, used by the manager UI and AssetPackService.
        await EnsureCustomBlueprintsLoadedAsync();
        return _customBlueprints;
    }

    public async Task<Blueprint> CreateCustomBlueprintAsync(CreateCustomBlueprintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Blueprint name is required.");
        }

        await EnsureCustomBlueprintsLoadedAsync();

        var newBlueprint = new Blueprint
        {
            Guid = Guid.NewGuid(),
            Id = AssignNextLoadedCustomBlueprintId(),
            IsLoaded = true,
            SourcePackId = "Local",
            LastModified = DateTime.UtcNow,
            IsPlayerCreated = true,
            BlueprintName = request.Name,
            BlueprintDescription = request.Description,
            BlueprintCost = request.Cost,
            UnlockClassId = request.UnlockClassId,
            UnlockFeatureId = request.UnlockFeatureId
        };

        _customBlueprints.Add(newBlueprint);
        await _customBlueprintStorage.SaveCustomBlueprintAsync(newBlueprint);

        return newBlueprint;
    }

    public async Task<bool> UpdateBlueprintAsync(Blueprint blueprint)
    {
        await EnsureCustomBlueprintsLoadedAsync();

        var existing = _customBlueprints.FirstOrDefault(b => b.Guid == blueprint.Guid);
        if (existing == null) return false;

        int index = _customBlueprints.IndexOf(existing);
        _customBlueprints[index] = blueprint;

        blueprint.LastModified = DateTime.UtcNow;
        await _customBlueprintStorage.SaveCustomBlueprintAsync(blueprint);
        return true;
    }

    public async Task<bool> DeleteBlueprintAsync(Guid guid)
    {
        await EnsureCustomBlueprintsLoadedAsync();

        var existing = _customBlueprints.FirstOrDefault(b => b.Guid == guid);
        if (existing == null) return false;

        _customBlueprints.Remove(existing);
        await _customBlueprintStorage.DeleteCustomBlueprintAsync(guid);
        return true;
    }

    public async Task<bool> LoadBlueprintAsync(Guid guid)
    {
        await EnsureCustomBlueprintsLoadedAsync();

        var blueprint = _customBlueprints.FirstOrDefault(b => b.Guid == guid);
        if (blueprint == null) return false;

        blueprint.IsLoaded = true;
        blueprint.Id = AssignNextLoadedCustomBlueprintId();

        await _customBlueprintStorage.SaveCustomBlueprintsAsync(_customBlueprints);
        return true;
    }

    public async Task<bool> UnloadBlueprintAsync(Guid guid)
    {
        await EnsureCustomBlueprintsLoadedAsync();

        var blueprint = _customBlueprints.FirstOrDefault(b => b.Guid == guid);
        if (blueprint == null) return false;

        blueprint.IsLoaded = false;
        blueprint.Id = 0;

        await _customBlueprintStorage.SaveCustomBlueprintsAsync(_customBlueprints);
        return true;
    }

    // Shared private helper — scans only currently-loaded custom blueprints for the
    // max ID, returns +1 (floor 60001). Mirrors the other data services' ID assignment.
    private int AssignNextLoadedCustomBlueprintId()
    {
        var maxId = _customBlueprints
            .Where(b => b.IsLoaded)
            .Select(b => b.Id)
            .DefaultIfEmpty(Blueprint.CUSTOM_BLUEPRINT_BASE)
            .Max();

        return Math.Max(maxId + 1, Blueprint.CUSTOM_BLUEPRINT_START);
    }
}