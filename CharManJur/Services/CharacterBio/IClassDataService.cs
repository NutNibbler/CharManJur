using CharManJur.Models;

namespace CharManJur.Services;

public interface IClassDataService
{
    Task<List<CharacterClass>> GetClassesAsync();
    Task<CharacterClass?> GetClassByIdAsync(int id);
}