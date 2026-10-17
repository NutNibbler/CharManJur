using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IHinderanceDataService
{
    Task<List<Hinderance>> GetHinderancesAsync();
    Task<Hinderance?> GetHinderanceByIdAsync(int id);
    Task<Hinderance?> GetHinderanceByGuidAsync(Guid guid);
    Task<Hinderance> CreateCustomHinderanceAsync(CreateCustomHinderanceRequest request);
    Task<bool> UpdateHinderanceAsync(Hinderance hinderance);
    Task<bool> DeleteHinderanceAsync(Guid guid);

    // == CUSTOM HINDERANCE TRACKER ===
    // Unfiltered — everything regardless of IsLoaded state. Used by the Asset Manager
    // UI and by AssetPackService internals, same role as ItemDataService.GetCustomItemsAsync().
    Task<List<Hinderance>> GetCustomHinderancesAsync();

    // === LOADER METHODS ===
    Task<bool> LoadHinderanceAsync(Guid guid);
    Task<bool> UnloadHinderanceAsync(Guid guid);
}

public class CreateCustomHinderanceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;
    public List<HinderanceSkillModifier> SkillModifiers { get; set; } = new();
}