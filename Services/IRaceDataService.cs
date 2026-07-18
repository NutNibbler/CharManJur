using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IRaceDataService
{
    Task<List<Race>> GetRacesAsync();
    Task<Race?> GetRaceByIdAsync(int id);
}