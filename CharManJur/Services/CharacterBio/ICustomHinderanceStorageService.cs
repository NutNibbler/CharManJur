using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomHinderanceStorageService
{
    Task<List<Hinderance>> LoadCustomHinderancesAsync();
    Task SaveCustomHinderanceAsync(Hinderance hinderance);
    Task SaveCustomHinderancesAsync(List<Hinderance> hinderances);
    Task<bool> DeleteCustomHinderanceAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}