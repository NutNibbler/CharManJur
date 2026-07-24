using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IBackgroundDataService
{
    Task<List<CharacterBackground>> GetBackgroundsAsync();
    Task<CharacterBackground?> GetBackgroundByIdAsync(int id);
    Task<List<CharacterBackground>> GetAllBackgroundsAsync(); // NEW: includes custom
    Task<CharacterBackground?> GetBackgroundByIdCombinedAsync(int id); // NEW: checks both
}