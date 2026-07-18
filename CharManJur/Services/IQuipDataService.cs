using System.Collections.Generic;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public interface IQuipDataService
{
    Task<List<Quip>> GetQuipsAsync();
    Task<List<Quip>> GetQuipsByFeatureIdAsync(int featureId);
    Task<List<Quip>> GetQuipsByClassIdAsync(int classId);
    Task<List<Quip>> GetQuipsForCharacterAsync(int classId, int featureId);
    Task<Quip?> GetQuipByIdAsync(int id);
}