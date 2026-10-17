using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class TraitDataService : ITraitDataService
{
    private readonly ICustomTraitStorageService _storage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _loaded;
    private List<CustomTrait> _traits = new();

    public TraitDataService(ICustomTraitStorageService storage)
    {
        _storage = storage;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_loaded) return;

            var loaded = await _storage.LoadCustomTraitsAsync();
            bool needsResave = false;

            foreach (var trait in loaded)
            {
                if (trait.Guid == Guid.Empty)
                {
                    trait.Guid = Guid.NewGuid();
                    needsResave = true;
                }
            }

            _traits = loaded;
            if (needsResave) await _storage.SaveCustomTraitsAsync(_traits);

            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<List<CustomTrait>> GetCustomTraitsAsync()
    {
        await EnsureLoadedAsync();
        return _traits;
    }

    public async Task<List<CustomTrait>> GetLoadedTraitsAsync()
    {
        await EnsureLoadedAsync();
        return _traits.Where(t => t.IsLoaded).ToList();
    }

    public async Task<CustomTrait?> GetTraitByGuidAsync(Guid guid)
    {
        await EnsureLoadedAsync();
        return _traits.FirstOrDefault(t => t.Guid == guid);
    }

    public async Task<CustomTrait> CreateCustomTraitAsync(CreateCustomTraitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Trait name is required.");
        }

        await EnsureLoadedAsync();

        var trait = new CustomTrait
        {
            Guid = Guid.NewGuid(),
            IsLoaded = true,
            SourcePackId = "Local",
            LastModified = DateTime.UtcNow,
            IsPlayerCreated = true,
            TraitName = request.Name.Trim(),
            PD = Math.Clamp(request.PD, 1, 4),
            Effect = request.Effect?.Trim() ?? string.Empty,
            Vigor = request.Vigor,
            Agility = request.Agility,
            Armor = Math.Clamp(request.Armor, 0, 3),
            WeaponDie = request.WeaponDie,
            WeaponType = request.WeaponType,
            Blast = request.Blast,
            Element = request.Element,
            ChooseElement = request.ChooseElement,
            IsSmall = request.IsSmall,
            IsLarge = request.IsLarge,
            RequiresSmall = request.RequiresSmall,
            ForbidsLarge = request.ForbidsLarge,
            GrantsPack = request.GrantsPack
        };

        _traits.Add(trait);
        await _storage.SaveCustomTraitAsync(trait);
        return trait;
    }

    public async Task<bool> UpdateTraitAsync(CustomTrait trait)
    {
        await EnsureLoadedAsync();

        var existing = _traits.FirstOrDefault(t => t.Guid == trait.Guid);
        if (existing == null) return false;

        int index = _traits.IndexOf(existing);
        _traits[index] = trait;

        trait.LastModified = DateTime.UtcNow;
        await _storage.SaveCustomTraitAsync(trait);
        return true;
    }

    public async Task<bool> DeleteTraitAsync(Guid guid)
    {
        await EnsureLoadedAsync();

        var existing = _traits.FirstOrDefault(t => t.Guid == guid);
        if (existing == null) return false;

        _traits.Remove(existing);
        await _storage.DeleteCustomTraitAsync(guid);
        return true;
    }

    public async Task<bool> LoadTraitAsync(Guid guid)
    {
        await EnsureLoadedAsync();

        var trait = _traits.FirstOrDefault(t => t.Guid == guid);
        if (trait == null) return false;

        trait.IsLoaded = true;
        await _storage.SaveCustomTraitsAsync(_traits);
        return true;
    }

    public async Task<bool> UnloadTraitAsync(Guid guid)
    {
        await EnsureLoadedAsync();

        var trait = _traits.FirstOrDefault(t => t.Guid == guid);
        if (trait == null) return false;

        trait.IsLoaded = false;
        await _storage.SaveCustomTraitsAsync(_traits);
        return true;
    }
}