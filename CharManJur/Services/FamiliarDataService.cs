using CharManJur.Models;
using CharManJur.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FamiliarDataService : IFamiliarDataService
{
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

    public Task<List<Familiar>> GetAllFamiliarsAsync()
    {
        return Task.FromResult(_familiars);
    }

    public Task<Familiar?> GetFamiliarByIdAsync(int id)
    {
        var result = _familiars.FirstOrDefault(f => f.Id == id);
        return Task.FromResult(result);
    }

    public Task<List<Familiar>> QueryFamiliarsAsync(FamiliarQueryCriteria criteria)
    {
        var query = _familiars.AsQueryable();

        if (criteria.SpecificFamiliarIds != null && criteria.SpecificFamiliarIds.Any())
        {
            query = query.Where(f => criteria.SpecificFamiliarIds.Contains(f.Id));
            return Task.FromResult(query.ToList());
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
                // Fallback: try parsing with exact match for debugging
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
        return Task.FromResult(result);
    }

    public Task<Familiar> CreateCustomFamiliarAsync(CreateCustomFamiliarRequest request)
    {
        var maxCustomId = _familiars
            .Where(f => f.IsPlayerCreated)
            .Select(f => f.Id)
            .DefaultIfEmpty(99000)
            .Max();

        int newId = Math.Max(maxCustomId + 1, 99001);

        var newFamiliar = new Familiar
        {
            Id = newId,
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
            IsPlayerCreated = true,
            IsFoundation = false
        };

        _familiars.Add(newFamiliar);
        return Task.FromResult(newFamiliar);
    }

    public Task<bool> UpdateFamiliarAsync(Familiar familiar)
    {
        var existing = _familiars.FirstOrDefault(f => f.Id == familiar.Id);
        if (existing == null) return Task.FromResult(false);

        var index = _familiars.IndexOf(existing);
        _familiars[index] = familiar;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteFamiliarAsync(int id)
    {
        var familiar = _familiars.FirstOrDefault(f => f.Id == id);
        if (familiar == null) return Task.FromResult(false);

        _familiars.Remove(familiar);
        return Task.FromResult(true);
    }
}