using CharManJur.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharManJur.Services;

public class LanguageDataService : ILanguageDataService
{
    private readonly List<Language> _languages = new()
    {
        new Language
        {
            Id = 1,
            Name = "Damarian",
            Description = "TThe kin of Vermil migrated into the redlands from the Eko-Damar Mountains centuries ago. The language has changed much since then, but is still close enough that if you find some ancient Damarian ruins, you might be able to piece together any literature you find. "
        },
        new Language
        {
            Id = 2,
            Name = "Pigin",
            Description = "Trader-tongue. The most widely spoken language in Tiver. While nearly everyone who lives in a city may speak pidgin, in a town there may only be a handful, and in a village there may only be one, if any at all. "
        },
        new Language
        {
            Id = 3,
            Name = "Fallar",
            Description = "This is the primary language in Fallier. There’s a lot of trade and general interaction/immigration between the two regions. "
        },
        new Language
        {
            Id = 4,
            Name = "Solek",
            Description = "Solek kin dwell in forested marshes. The language has many different dialects but the mechanics remain the same regardless of whichever tribe you interact with. "
        },
        new Language
        {
            Id = 5,
            Name = "Dwarf",
            Description = "Ysol is predominantly Dwarvish, but the ancient language changed a lot over the millenia into modern Damarian. Istador, however, mandates that the primary language of the region be ancient Dwarf and all are forced to learn."
        },
        new Language
        {
            Id = 6,
            Name = "Fae",
            Description = "Void leylines that scatter the redlands remind travellers that the Fae came from elsewhere at the turn of the millennium."
        },
        new Language
        {
            Id = 7,
            Name = "Rruen",
            Description = "The Rruen navy has a presence in nearly every port city willing to grant them an embassy. "
        }
    };

    public Task<List<Language>> GetLanguagesAsync()
    {
        return Task.FromResult(_languages);
    }

    public Task<Language?> GetLanguageByIdAsync(int id)
    {
        var result = _languages.FirstOrDefault(l => l.Id == id);
        return Task.FromResult(result);
    }
}