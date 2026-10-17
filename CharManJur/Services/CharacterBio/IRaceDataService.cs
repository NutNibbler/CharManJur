using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IRaceDataService
{
    Task<List<Race>> GetRacesAsync();
    Task<Race?> GetRaceByIdAsync(int id);
    Task<List<Race>> GetAllRacesAsync(); // includes custom races, filtered to IsLoaded == true
    Task<Race?> GetRaceByIdCombinedAsync(int id); // checks both sources, filtered to IsLoaded == true
    Task<Race?> GetRaceByGuidAsync(Guid guid);
    Task<Race> CreateCustomRaceAsync(CreateCustomRaceRequest request);
    Task<bool> UpdateRaceAsync(Race race);
    Task<bool> DeleteRaceAsync(Guid guid);

    // == CUSTOM RACE TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Race>> GetCustomRacesAsync();

    // === LOADER METHODS ===
    Task<bool> LoadRaceAsync(Guid guid);
    Task<bool> UnloadRaceAsync(Guid guid);
}

public class CreateCustomRaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FeatureName { get; set; }
    public string? FeatureDescription { get; set; }
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;
    public List<RaceSkillBonus> SkillBonuses { get; set; } = new();
    public List<PrehensileLimbSet> LimbSets { get; set; } = new();
    public List<string>? CompatibleCampaigns { get; set; }
}