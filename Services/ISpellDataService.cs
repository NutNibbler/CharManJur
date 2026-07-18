using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface ISpellDataService
{
    Task<List<Spell>> GetSpellsAsync();
    Task<List<Spell>> GetSpellsByFeatureIdAsync(int featureId);
    Task<List<Spell>> GetSpellsByClassIdAsync(int classId);
    Task<List<Spell>> GetSpellsForCharacterAsync(int classId, int featureId);
    Task<Spell?> GetSpellByIdAsync(int id);
}