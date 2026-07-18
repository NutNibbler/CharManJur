using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IBackgroundDataService
{
    Task<List<CharacterBackground>> GetBackgroundsAsync();
    Task<CharacterBackground?> GetBackgroundByIdAsync(int id);
}