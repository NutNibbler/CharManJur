using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomRaceStorageService
{
    Task<List<Race>> LoadCustomRacesAsync();
    Task SaveCustomRaceAsync(Race race);
    Task SaveCustomRacesAsync(List<Race> races);
    Task<bool> DeleteCustomRaceAsync(int id);
    Task<int> GetNextCustomRaceIdAsync();
    Task<string> GetStoragePathAsync();
    Task<bool> CustomRaceExistsAsync(int id);
}