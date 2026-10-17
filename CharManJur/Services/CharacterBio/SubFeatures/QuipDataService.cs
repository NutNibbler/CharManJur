using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class QuipDataService : IQuipDataService
{
    private readonly ICustomQuipStorageService _customQuipStorage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _customQuipsLoaded = false;
    private List<Quip> _customQuips = new();

    private readonly List<Quip> _quips = new()
    {
        new Quip
        {
            Id = 1,
            QuipName = "Book Leaf",
            QuipDescription = "The caster can magically open a book within 10 ft. and leaf through its pages without touching it. "
        },
        new Quip
        {
            Id = 2,
            QuipName = "Cleaning Brush",
            QuipDescription = "A broom or mop takes on a life of its own and cleans an area designated by the caster. "
        },
        new Quip
        {
            Id = 3,
            QuipName = "Coloured Flame",
            QuipDescription = "A flame within range is imbued with a hue of the caster’s choosing. "
        },
        new Quip
        {
            Id = 4,
            QuipName = "Floating Trinket",
            QuipDescription = "The caster causes a small possession of 3lbs or less within range to levitate and float through the air at up to 1’ per round. Can only float, can’t be used. "
        },
        new Quip
        {
            Id = 5,
            QuipName = "Magic Quill",
            QuipDescription = "A quill floats and moves of its own accord, magically transcribing the caster’s words onto a page. "
        },
        new Quip
        {
            Id = 6,
            QuipName = "Open/Close Portal",
            QuipDescription = "An unlocked portal within range swings open or shut as the caster desires. "
        },
        new Quip
        {
            Id = 7,
            QuipName = "Rune",
            QuipDescription = "The caster traces a glowing sigil in the air or on a surface or object. A caster’s rune is unique and can be used by another caster to identify them with context."
        },
        new Quip
        {
            Id = 8,
            QuipName = "Sense Magic",
            QuipDescription = "The caster attunes their mind to subtle arcane energies, attempting to detect magic on a creature or object touched. There is a chance this fails, you will never know… until you do. "
        },
        new Quip
        {
            Id = 9,
            QuipName = "Smoke Rings",
            QuipDescription = "While smoking a pipe, the caster gains the ability to blow impressive smoke rings of any colour desired. "
        },
        new Quip
        {
            Id = 10,
            QuipName = "Spark",
            QuipDescription = "A tiny spark of flame flashes onto the caster’s fingertip. 5’ radius light. Can ignite flammable material. "
        },
        new Quip
        {
            Id = 11,
            QuipName = "Vanish",
            QuipDescription = "A small object touched by the caster becomes invisible for 1 round. No more than 3lbs. "
        }
    };

    public QuipDataService(ICustomQuipStorageService customQuipStorage)
    {
        _customQuipStorage = customQuipStorage;
    }

    private async Task EnsureCustomQuipsLoadedAsync()
    {
        if (_customQuipsLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_customQuipsLoaded) return;

            var loaded = await _customQuipStorage.LoadCustomQuipsAsync();
            bool needsResave = false;

            foreach (var quip in loaded)
            {
                // One-time migration: anything still at Guid.Empty (pre-migration saves)
                // gets a real Guid assigned and the file gets re-saved. Same pattern as
                // the other data services' migration pass.
                if (quip.Guid == Guid.Empty)
                {
                    quip.Guid = Guid.NewGuid();
                    needsResave = true;
                }
            }

            _customQuips = loaded;

            if (needsResave)
            {
                await _customQuipStorage.SaveCustomQuipsAsync(_customQuips);
            }

            _customQuipsLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Task<List<Quip>> GetQuipsAsync()
    {
        return Task.FromResult(_quips);
    }

    public Task<List<Quip>> GetQuipsByFeatureIdAsync(int featureId)
    {
        var result = _quips.Where(q => q.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Quip>> GetQuipsByClassIdAsync(int classId)
    {
        var result = _quips.Where(q => q.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public async Task<List<Quip>> GetQuipsForCharacterAsync(int classId, int featureId)
    {
        await EnsureCustomQuipsLoadedAsync();

        var combined = new List<Quip>(_quips);
        combined.AddRange(_customQuips.Where(q => q.IsLoaded));

        var result = combined.Where(q =>
            q.UnlockClassId == classId ||
            q.UnlockFeatureId == featureId)
            .ToList();

        return result;
    }

    public Task<Quip?> GetQuipByIdAsync(int id)
    {
        var foundation = _quips.FirstOrDefault(q => q.Id == id);
        if (foundation != null) return Task.FromResult<Quip?>(foundation);

        var custom = _customQuips.FirstOrDefault(q => q.Id == id);
        return Task.FromResult(custom);
    }

    public async Task<Quip?> GetQuipByGuidAsync(Guid guid)
    {
        await EnsureCustomQuipsLoadedAsync();
        return _customQuips.FirstOrDefault(q => q.Guid == guid);
    }

    public async Task<List<Quip>> GetCustomQuipsAsync()
    {
        // Deliberately unfiltered — the Asset Manager's own "show everything
        // regardless of state" method, used by the manager UI and AssetPackService.
        await EnsureCustomQuipsLoadedAsync();
        return _customQuips;
    }

    public async Task<Quip> CreateCustomQuipAsync(CreateCustomQuipRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Quip name is required.");
        }

        await EnsureCustomQuipsLoadedAsync();

        var newQuip = new Quip
        {
            Guid = Guid.NewGuid(),
            Id = AssignNextLoadedCustomQuipId(),
            IsLoaded = true,
            SourcePackId = "Local",
            LastModified = DateTime.UtcNow,
            IsPlayerCreated = true,
            QuipName = request.Name,
            QuipDescription = request.Description,
            UnlockClassId = request.UnlockClassId,
            UnlockFeatureId = request.UnlockFeatureId
        };

        _customQuips.Add(newQuip);
        await _customQuipStorage.SaveCustomQuipAsync(newQuip);

        return newQuip;
    }

    public async Task<bool> UpdateQuipAsync(Quip quip)
    {
        await EnsureCustomQuipsLoadedAsync();

        var existing = _customQuips.FirstOrDefault(q => q.Guid == quip.Guid);
        if (existing == null) return false;

        int index = _customQuips.IndexOf(existing);
        _customQuips[index] = quip;

        quip.LastModified = DateTime.UtcNow;
        await _customQuipStorage.SaveCustomQuipAsync(quip);
        return true;
    }

    public async Task<bool> DeleteQuipAsync(Guid guid)
    {
        await EnsureCustomQuipsLoadedAsync();

        var existing = _customQuips.FirstOrDefault(q => q.Guid == guid);
        if (existing == null) return false;

        _customQuips.Remove(existing);
        await _customQuipStorage.DeleteCustomQuipAsync(guid);
        return true;
    }

    public async Task<bool> LoadQuipAsync(Guid guid)
    {
        await EnsureCustomQuipsLoadedAsync();

        var quip = _customQuips.FirstOrDefault(q => q.Guid == guid);
        if (quip == null) return false;

        quip.IsLoaded = true;
        quip.Id = AssignNextLoadedCustomQuipId();

        await _customQuipStorage.SaveCustomQuipsAsync(_customQuips);
        return true;
    }

    public async Task<bool> UnloadQuipAsync(Guid guid)
    {
        await EnsureCustomQuipsLoadedAsync();

        var quip = _customQuips.FirstOrDefault(q => q.Guid == guid);
        if (quip == null) return false;

        quip.IsLoaded = false;
        quip.Id = 0;

        await _customQuipStorage.SaveCustomQuipsAsync(_customQuips);
        return true;
    }

    // Shared private helper — scans only currently-loaded custom quips for the
    // max ID, returns +1 (floor 40001). Mirrors the other data services' ID assignment.
    private int AssignNextLoadedCustomQuipId()
    {
        var maxId = _customQuips
            .Where(q => q.IsLoaded)
            .Select(q => q.Id)
            .DefaultIfEmpty(Quip.CUSTOM_QUIP_BASE)
            .Max();

        return Math.Max(maxId + 1, Quip.CUSTOM_QUIP_START);
    }
}