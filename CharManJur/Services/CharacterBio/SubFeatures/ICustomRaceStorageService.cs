using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomRaceStorageService
{
    Task<List<Race>> LoadCustomRacesAsync();
    Task SaveCustomRaceAsync(Race race);
    Task SaveCustomRacesAsync(List<Race> races);
    Task<bool> DeleteCustomRaceAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}