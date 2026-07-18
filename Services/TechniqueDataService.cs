using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class TechniqueDataService : ITechniqueDataService
{
    private readonly List<Technique> _techniques = new()
    {
        new Technique
        {
            Id = 1,
            TechniqueName = "Range",
            TechniqueDescription = "Invest +1 MD to reach a target far away."
        },
        new Technique
        {
            Id = 2,
            TechniqueName = "Elements",
            TechniqueDescription = "Invest +1 MD to imbue your spell with a particular element, i.e. fire, ice, lightning."
        },
        new Technique
        {
            Id = 3,
            TechniqueName = "Subtlety",
            TechniqueDescription = "Invest +1 MD to cast a silent spell."
        },
        new Technique
        {
            Id = 4,
            TechniqueName = "Force",
            TechniqueDescription = "Invest +1 MD to push your targets back."
        },
        new Technique
        {
            Id = 5,
            TechniqueName = "Pierce",
            TechniqueDescription = "Invest +1 MD to pierce armor. On a 1-3 you bypass 1 armor, on a 4-6 you bypass 2. "
        },
        new Technique
        {
            Id = 6,
            TechniqueName = "Split",
            TechniqueDescription = "Invest +1 MD to split your spell (including effects) to 2 different targets. "
        },
        new Technique
        {
            Id = 7,
            TechniqueName = "Blast",
            TechniqueDescription = "Invest +1 MD to change the properties of the energy to the Blast property. The invested dice determine how many targets are affected within close range of a specific point. "
        },
        new Technique
        {
            Id = 8,
            TechniqueName = "Fast",
            TechniqueDescription = "Invest +1 MD to cast the spell as 1 action instead of 2, allowing the spell to be a quick turn."
        }
    };

    public Task<List<Technique>> GetTechniquesAsync()
    {
        return Task.FromResult(_techniques);
    }

    public Task<List<Technique>> GetTechniquesByFeatureIdAsync(int featureId)
    {
        var result = _techniques.Where(t => t.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Technique>> GetTechniquesByClassIdAsync(int classId)
    {
        var result = _techniques.Where(t => t.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Technique>> GetTechniquesForCharacterAsync(int classId, int featureId)
    {
        var result = _techniques.Where(t =>
            t.UnlockClassId == classId ||
            t.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<Technique?> GetTechniqueByIdAsync(int id)
    {
        var result = _techniques.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(result);
    }
}