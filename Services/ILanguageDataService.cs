using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ILanguageDataService
{
    Task<List<Language>> GetLanguagesAsync();
    Task<Language?> GetLanguageByIdAsync(int id);
}