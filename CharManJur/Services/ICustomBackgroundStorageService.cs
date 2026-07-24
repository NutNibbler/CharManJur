using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomBackgroundStorageService
{
    Task<List<CharacterBackground>> LoadCustomBackgroundsAsync();
    Task SaveCustomBackgroundAsync(CharacterBackground background);
    Task SaveCustomBackgroundsAsync(List<CharacterBackground> backgrounds);
    Task<bool> DeleteCustomBackgroundAsync(int id);
    Task<int> GetNextCustomBackgroundIdAsync();
    Task<string> GetStoragePathAsync();
    Task<bool> CustomBackgroundExistsAsync(int id);
}