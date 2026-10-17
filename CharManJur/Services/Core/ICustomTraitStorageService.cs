using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomTraitStorageService
{
    Task<List<CustomTrait>> LoadCustomTraitsAsync();
    Task SaveCustomTraitAsync(CustomTrait trait);
    Task SaveCustomTraitsAsync(List<CustomTrait> traits);
    Task<bool> DeleteCustomTraitAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}