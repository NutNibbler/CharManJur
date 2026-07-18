using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface IBlueprintDataService
{
    Task<List<Blueprint>> GetBlueprintsAsync();
    Task<List<Blueprint>> GetBlueprintsByFeatureIdAsync(int featureId);
    Task<List<Blueprint>> GetBlueprintsByClassIdAsync(int classId);
    Task<List<Blueprint>> GetBlueprintsForCharacterAsync(int classId, int featureId);
    Task<Blueprint?> GetBlueprintByIdAsync(int id);
}