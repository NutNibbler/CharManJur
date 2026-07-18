using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IHinderanceDataService
{
    Task<List<Hinderance>> GetHinderancesAsync();
    Task<Hinderance?> GetHinderanceByIdAsync(int id);
}