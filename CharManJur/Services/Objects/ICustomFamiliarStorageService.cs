using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomFamiliarStorageService
{
    Task<List<Familiar>> LoadCustomFamiliarsAsync();
    Task SaveCustomFamiliarAsync(Familiar familiar);
    Task SaveCustomFamiliarsAsync(List<Familiar> familiars);
    Task<bool> DeleteCustomFamiliarAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}