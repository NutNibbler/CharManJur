using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IHinderanceDataService
{
    Task<List<Hinderance>> GetHinderancesAsync();
    Task<Hinderance?> GetHinderanceByIdAsync(int id);
    Task<Hinderance> CreateCustomHinderanceAsync(CreateCustomHinderanceRequest request);
    Task<bool> UpdateHinderanceAsync(Hinderance hinderance);
    Task<bool> DeleteHinderanceAsync(int id);
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