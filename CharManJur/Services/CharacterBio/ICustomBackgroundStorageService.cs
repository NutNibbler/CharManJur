using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomBackgroundStorageService
{
    Task<List<CharacterBackground>> LoadCustomBackgroundsAsync();
    Task SaveCustomBackgroundAsync(CharacterBackground background);
    Task SaveCustomBackgroundsAsync(List<CharacterBackground> backgrounds);
    Task<bool> DeleteCustomBackgroundAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}