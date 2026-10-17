using CharManJur.Models;
using CharManJur.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class FamiliarDataService : IFamiliarDataService
{
    private readonly ICustomFamiliarStorageService _customFamiliarStorage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _customFamiliarsLoaded = false;

    private readonly List<Familiar> _familiars = new()
    {
        new Familiar
        {
            Id = 1,
            FmlrName = "Raven",
            FmlrClass = FmlrClasses.Aves,
            FmlrSize = FmlrSizes.Tiny,
            FmlrIntelligence = FmlrIntelligences.SemiDomestic,
            HP = 2,
            StatVigor = 3,
            StatAgility = 16,
            StatMind = 10,
            StatSpirit = 8,
            IsFoundation = true,
            FmlrWeaponName = "Claw",
            FmlrWeaponSpeed = FmlrWeaponSpeeds.Balanced,
            FmlrWeaponDamageDie = FmlrWeaponDamageDies.D4
        },
        new Familiar
        {
            Id = 2,
            FmlrName = "Snake",
            FmlrClass = FmlrClasses.Reptilia,
            FmlrSize = FmlrSizes.Small,
            FmlrIntelligence = FmlrIntelligences.Wild,
            HP = 4,
            StatVigor = 5,
            StatAgility = 8,
            StatMind = 5,
            StatSpirit = 8,
            IsFoundation = true,
            FmlrWeaponName = "Fangs",
            FmlrWeaponSpeed = FmlrWeaponSpeeds.Fast,
            FmlrWeaponDamageDie = FmlrWeaponDamageDies.D4
        },
        new Familiar
        {
            Id = 3,
            FmlrName = "Rat",
            FmlrClass = FmlrClasses.Mammalia,
            FmlrSize = FmlrSizes.Tiny,
            FmlrIntelligence = FmlrIntelligences.SemiDomestic,
            HP = 3,
            StatVigor = 4,
            StatAgility = 12,
            StatMind = 12,
            StatSpirit = 8,
            IsFoundation = true,
            FmlrWeaponName = "Bite",
            FmlrWeaponSpeed = FmlrWeaponSpeeds.Balanced,
            FmlrWeaponDamageDie = FmlrWeaponDamageDies.D4
        },
        new Familiar
        {
            Id = 4,
            FmlrName = "Donkey",
            FmlrClass = FmlrClasses.Mammalia,
            FmlrSize = FmlrSizes.Large,
            FmlrIntelligence = FmlrIntelligences.SemiDomestic,
            HP = 5,
            StatVigor = 8,
            StatAgility = 8,
            StatMind = 10,
            StatSpirit = 8,
            IsFoundation = true,
            FmlrWeaponName = "Donkey Kick",
            FmlrWeaponSpeed = FmlrWeaponSpeeds.Slow,
            FmlrWeaponDamageDie = FmlrWeaponDamageDies.D6
        }
    };

    public FamiliarDataService(ICustomFamiliarStorageService customFamiliarStorage)
    {
        _customFamiliarStorage = customFamiliarStorage;
    }

    private async Task EnsureCustomFamiliarsLoadedAsync()
    {
        if (_customFamiliarsLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_customFamiliarsLoaded) return; // double-check after acquiring the lock

            var customFamiliars = await _customFamiliarStorage.LoadCustomFamiliarsAsync();
            bool needsResave = false;

            foreach (var familiar in customFamiliars)
            {
                // One-time migration: anything still at Guid.Empty (pre-migration saves)
                // gets a real Guid assigned and the file gets re-saved. Same pattern as
                // ItemDataService/BackgroundDataService's migration pass.
                if (familiar.Guid == Guid.Empty)
                {
                    familiar.Guid = Guid.NewGuid();
                    needsResave = true;
                }

                if (!_familiars.Any(f => f.Id == familiar.Id))
                {
                    _familiars.Add(familiar);
                }
            }

            if (needsResave)
            {
                await _customFamiliarStorage.SaveCustomFamiliarsAsync(customFamiliars);
            }

            _customFamiliarsLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<List<Familiar>> GetAllFamiliarsAsync()
    {
        await EnsureCustomFamiliarsLoadedAsync();
        return _familiars.Where(f => f.IsLoaded).ToList();
    }

    public async Task<Familiar?> GetFamiliarByIdAsync(int id)
    {
        await EnsureCustomFamiliarsLoadedAsync();
        return _familiars.FirstOrDefault(f => f.Id == id);
    }

    public async Task<Familiar?> GetFamiliarByGuidAsync(Guid guid)
    {
        await EnsureCustomFamiliarsLoadedAsync();
        return _familiars.FirstOrDefault(f => f.Guid == guid);
    }

    public async Task<List<Familiar>> QueryFamiliarsAsync(FamiliarQueryCriteria criteria)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var query = _familiars.Where(f => f.IsLoaded).AsQueryable();

        if (criteria.SpecificFamiliarIds != null && criteria.SpecificFamiliarIds.Any())
        {
            query = query.Where(f => criteria.SpecificFamiliarIds.Contains(f.Id));
            return query.ToList();
        }

        if (!string.IsNullOrEmpty(criteria.Species))
        {
            if (Enum.TryParse<FmlrClasses>(criteria.Species, true, out var speciesEnum))
            {
                query = query.Where(f => f.FmlrClass == speciesEnum);
            }
        }

        if (criteria.AllowedSpecies != null && criteria.AllowedSpecies.Any())
        {
            var speciesEnums = criteria.AllowedSpecies
                .Select(s => Enum.TryParse<FmlrClasses>(s, true, out var e) ? e : (FmlrClasses?)null)
                .Where(e => e.HasValue)
                .Select(e => e.Value)
                .ToList();

            if (speciesEnums.Any())
            {
                query = query.Where(f => speciesEnums.Contains(f.FmlrClass.Value));
            }
        }

        if (!string.IsNullOrEmpty(criteria.Size))
        {
            if (Enum.TryParse<FmlrSizes>(criteria.Size, true, out var sizeEnum))
            {
                query = query.Where(f => f.FmlrSize == sizeEnum);
            }
        }

        if (criteria.AllowedSizes != null && criteria.AllowedSizes.Any())
        {
            var sizeEnums = criteria.AllowedSizes
                .Select(s => Enum.TryParse<FmlrSizes>(s, true, out var e) ? e : (FmlrSizes?)null)
                .Where(e => e.HasValue)
                .Select(e => e.Value)
                .ToList();

            if (sizeEnums.Any())
            {
                query = query.Where(f => sizeEnums.Contains(f.FmlrSize));
            }
        }

        if (!string.IsNullOrEmpty(criteria.Intelligence))
        {
            if (Enum.TryParse<FmlrIntelligences>(criteria.Intelligence, true, out var intelEnum))
            {
                query = query.Where(f => f.FmlrIntelligence == intelEnum);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse Intelligence: {criteria.Intelligence}");
            }
        }

        if (criteria.AllowedIntelligences != null && criteria.AllowedIntelligences.Any())
        {
            var intelEnums = criteria.AllowedIntelligences
                .Select(s => Enum.TryParse<FmlrIntelligences>(s, true, out var e) ? e : (FmlrIntelligences?)null)
                .Where(e => e.HasValue)
                .Select(e => e.Value)
                .ToList();

            if (intelEnums.Any())
            {
                query = query.Where(f => intelEnums.Contains(f.FmlrIntelligence.Value));
            }
        }

        if (!criteria.IncludePlayerCreated)
            query = query.Where(f => !f.IsPlayerCreated);

        if (!criteria.IncludeFoundation)
            query = query.Where(f => f.SourceCampaignId != null);

        if (criteria.CampaignId != null)
            query = query.Where(f => f.SourceCampaignId == criteria.CampaignId);

        if (criteria.MaxResults.HasValue)
            query = query.Take(criteria.MaxResults.Value);

        var result = query.ToList();
        System.Diagnostics.Debug.WriteLine($"QueryFamiliarsAsync: Found {result.Count} familiars");
        return result;
    }

    public async Task<Familiar> CreateCustomFamiliarAsync(CreateCustomFamiliarRequest request)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var newFamiliar = new Familiar
        {
            Guid = Guid.NewGuid(),
            Id = AssignNextLoadedCustomFamiliarId(),
            IsLoaded = true,
            SourcePackId = "Local",
            LastModified = DateTime.UtcNow,
            FmlrName = request.Name,
            FmlrDescription = request.Description,
            FmlrClass = request.FmlrClass,
            FmlrSize = request.FmlrSize,
            FmlrIntelligence = request.Intelligence,
            HP = request.HP,
            StatVigor = request.StatVigor,
            StatAgility = request.StatAgility,
            StatMind = request.StatMind,
            StatSpirit = request.StatSpirit,
            FmlrWeaponName = request.WeaponName,
            FmlrWeaponSpeed = request.WeaponSpeed,
            FmlrWeaponDamageDie = request.WeaponDamageDie,
            Abilities = request.Abilities ?? new List<string>(),
            BaseInventoryCapacity = request.BaseInventoryCapacity,
            IsPlayerCreated = true,
            IsFoundation = false
        };

        _familiars.Add(newFamiliar);
        await _customFamiliarStorage.SaveCustomFamiliarAsync(newFamiliar);

        return newFamiliar;
    }

    public async Task<bool> UpdateFamiliarAsync(Familiar familiar)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var existing = _familiars.FirstOrDefault(f => f.Guid == familiar.Guid);
        if (existing == null) return false;

        var index = _familiars.IndexOf(existing);
        _familiars[index] = familiar;

        if (familiar.IsPlayerCreated)
        {
            familiar.LastModified = DateTime.UtcNow;
            await _customFamiliarStorage.SaveCustomFamiliarAsync(familiar);
        }

        return true;
    }

    public async Task<bool> DeleteFamiliarAsync(Guid guid)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var familiar = _familiars.FirstOrDefault(f => f.Guid == guid);
        if (familiar == null) return false;

        _familiars.Remove(familiar);

        if (familiar.IsPlayerCreated)
        {
            await _customFamiliarStorage.DeleteCustomFamiliarAsync(guid);
        }

        return true;
    }

    public async Task<List<Familiar>> GetCustomFamiliarsAsync()
    {
        // Deliberately unfiltered — the Asset Manager's own "show everything
        // regardless of state" method, used by the manager UI and AssetPackService.
        await EnsureCustomFamiliarsLoadedAsync();
        return _familiars.Where(f => f.IsPlayerCreated).ToList();
    }

    public async Task<bool> LoadFamiliarAsync(Guid guid)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var familiar = _familiars.FirstOrDefault(f => f.Guid == guid);
        if (familiar == null || !familiar.IsPlayerCreated) return false;

        familiar.IsLoaded = true;
        familiar.Id = AssignNextLoadedCustomFamiliarId();

        await _customFamiliarStorage.SaveCustomFamiliarsAsync(_familiars.Where(f => f.IsPlayerCreated).ToList());
        return true;
    }

    public async Task<bool> UnloadFamiliarAsync(Guid guid)
    {
        await EnsureCustomFamiliarsLoadedAsync();

        var familiar = _familiars.FirstOrDefault(f => f.Guid == guid);
        if (familiar == null || !familiar.IsPlayerCreated) return false;

        familiar.IsLoaded = false;
        familiar.Id = 0;

        await _customFamiliarStorage.SaveCustomFamiliarsAsync(_familiars.Where(f => f.IsPlayerCreated).ToList());
        return true;
    }

    // Shared private helper — scans only currently-loaded custom familiars for the
    // max ID, returns +1 (floor 80001). Mirrors AssignNextLoadedCustomItemId /
    // AssignNextLoadedCustomBackgroundId.
    private int AssignNextLoadedCustomFamiliarId()
    {
        var maxId = _familiars
            .Where(f => f.IsPlayerCreated && f.IsLoaded)
            .Select(f => f.Id)
            .DefaultIfEmpty(Familiar.CUSTOM_FAMILIAR_BASE)
            .Max();

        return Math.Max(maxId + 1, Familiar.CUSTOM_FAMILIAR_START);
    }
}