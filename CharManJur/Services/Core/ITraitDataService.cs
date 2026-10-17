using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface ITraitDataService
{
    // Unfiltered: everything regardless of IsLoaded. Used by the Asset Manager and by AssetPackService.
    Task<List<CustomTrait>> GetCustomTraitsAsync();

    // Only loaded traits: what the transformation screen offers.
    Task<List<CustomTrait>> GetLoadedTraitsAsync();

    Task<CustomTrait?> GetTraitByGuidAsync(Guid guid);
    Task<CustomTrait> CreateCustomTraitAsync(CreateCustomTraitRequest request);
    Task<bool> UpdateTraitAsync(CustomTrait trait);
    Task<bool> DeleteTraitAsync(Guid guid);
    Task<bool> LoadTraitAsync(Guid guid);
    Task<bool> UnloadTraitAsync(Guid guid);
}

public class CreateCustomTraitRequest
{
    public string Name { get; set; } = string.Empty;
    public int PD { get; set; } = 1;          // Polymorph Dice that must be invested to access the trait (1-4)
    public string Effect { get; set; } = string.Empty;

    // Everything below is optional.
    public int? Vigor { get; set; }
    public int? Agility { get; set; }
    public int Armor { get; set; }            // 0-3; the trait's name is shown as the armor's name
    public int? WeaponDie { get; set; }       // 4, 6, 8, 10, 12; null = no natural weapon
    public WeaponCategoryType? WeaponType { get; set; }
    public bool Blast { get; set; }
    public DamageElement Element { get; set; }
    public bool ChooseElement { get; set; }
    public bool IsSmall { get; set; }
    public bool IsLarge { get; set; }
    public bool RequiresSmall { get; set; }
    public bool ForbidsLarge { get; set; }
    public bool GrantsPack { get; set; }
}