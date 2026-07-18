using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface ITechniqueDataService
{
    Task<List<Technique>> GetTechniquesAsync();
    Task<List<Technique>> GetTechniquesByFeatureIdAsync(int featureId);
    Task<List<Technique>> GetTechniquesByClassIdAsync(int classId);
    Task<List<Technique>> GetTechniquesForCharacterAsync(int classId, int featureId);
    Task<Technique?> GetTechniqueByIdAsync(int id);
}