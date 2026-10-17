using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomTechniqueStorageService
{
    Task<List<Technique>> LoadCustomTechniquesAsync();
    Task SaveCustomTechniqueAsync(Technique technique);
    Task SaveCustomTechniquesAsync(List<Technique> techniques);
    Task<bool> DeleteCustomTechniqueAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}