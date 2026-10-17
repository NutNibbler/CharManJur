using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomSpellStorageService
{
    Task<List<Spell>> LoadCustomSpellsAsync();
    Task SaveCustomSpellAsync(Spell spell);
    Task SaveCustomSpellsAsync(List<Spell> spells);
    Task<bool> DeleteCustomSpellAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}