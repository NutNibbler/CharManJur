using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomBlueprintStorageService
{
    Task<List<Blueprint>> LoadCustomBlueprintsAsync();
    Task SaveCustomBlueprintAsync(Blueprint blueprint);
    Task SaveCustomBlueprintsAsync(List<Blueprint> blueprints);
    Task<bool> DeleteCustomBlueprintAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}