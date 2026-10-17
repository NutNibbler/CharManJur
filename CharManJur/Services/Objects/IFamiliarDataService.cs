using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IFamiliarDataService
{
    Task<List<Familiar>> GetAllFamiliarsAsync();
    Task<Familiar?> GetFamiliarByIdAsync(int id);
    Task<Familiar?> GetFamiliarByGuidAsync(Guid guid);
    Task<List<Familiar>> QueryFamiliarsAsync(FamiliarQueryCriteria criteria);
    Task<Familiar> CreateCustomFamiliarAsync(CreateCustomFamiliarRequest request);
    Task<bool> UpdateFamiliarAsync(Familiar familiar);
    Task<bool> DeleteFamiliarAsync(Guid guid);

    // == CUSTOM FAMILIAR TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Familiar>> GetCustomFamiliarsAsync();

    // === LOADER METHODS ===
    Task<bool> LoadFamiliarAsync(Guid guid);
    Task<bool> UnloadFamiliarAsync(Guid guid);

}

public class CreateCustomFamiliarRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FmlrClasses FmlrClass { get; set; }
    public FmlrSizes FmlrSize { get; set; }
    public FmlrIntelligences Intelligence { get; set; }
    public int BaseInventoryCapacity { get; set; }
    public int HP { get; set; } = 10;
    public int StatVigor { get; set; } = 10;
    public int StatAgility { get; set; } = 10;
    public int StatMind { get; set; } = 10;
    public int StatSpirit { get; set; } = 10;
    public string? WeaponName { get; set; }
    public FmlrWeaponSpeeds? WeaponSpeed { get; set; }
    public FmlrWeaponDamageDies? WeaponDamageDie { get; set; }
    public List<string>? Abilities { get; set; }
    public string? SourceCampaignId { get; set; }
    public int? SourcePlayerId { get; set; }
}