using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomFamiliarStorageService
{
    Task<List<Familiar>> LoadCustomFamiliarsAsync();
    Task SaveCustomFamiliarAsync(Familiar familiar);
    Task SaveCustomFamiliarsAsync(List<Familiar> familiars);
    Task<bool> DeleteCustomFamiliarAsync(int id);
    Task<int> GetNextCustomFamiliarIdAsync();
    Task<string> GetStoragePathAsync();
    Task<bool> CustomFamiliarExistsAsync(int id);
}