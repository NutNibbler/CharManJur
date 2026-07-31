using CharManJur.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CharManJur.Services;

public class HinderanceDataService : IHinderanceDataService
{
    private readonly ICustomHinderanceStorageService _customHinderanceStorage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _customHinderancesLoaded = false;

    private readonly List<Hinderance> _hinderances = new()
    {
        new Hinderance
        {
            Id = 1,
            Name = "Addict",
            Description = "Your substance of choice dictates your life choices to a degree. \r\nYour character is deprived until they are clean should they not use at least once a day. "
        },
        new Hinderance
        {
            Id = 2,
            Name = "Avarice",
            Description = "There is nothing you wouldn’t do for money and you believe everyone has a price. "
        },
        new Hinderance
        {
            Id = 3,
            Name = "Gossip",
            Description = "You can’t keep a secret. You’re a blabber mouth. You can’t be trusted to shut up. "
        },
        new Hinderance
        {
            Id = 4,
            Name = "Debt",
            Description = "You owe somebody and they are relentlessly pursuing what they are owed. "
        },
        new Hinderance
        {
            Id = 5,
            Name = "Promiscuous",
            Description = "You may have some children elsewhere. With a few different people. Or an STD. Regardless, you like to fuck and it gets in the way sometimes."
        },
        new Hinderance
        {
            Id = 6,
            Name = "Ruthless",
            Description = "You bear no mercy for your enemies. You kill. You always kill. With brutality. "
        },
        new Hinderance
        {
            Id = 7,
            Name = "Nemesis",
            Description = "You are ruthlessly hunted by someone for some reason. You decide. "
        },
        new Hinderance
        {
            Id = 8,
            Name = "Phobia",
            Description = "Select a phobia. It can be one from our reality, or it can be one you think is uniquely Tiverian. When subjected to your phobia, you make morale saves or flee."
        },
        new Hinderance
        {
            Id = 9,
            Name = "Pacifist",
            Description = "Thou shalt not kill… or hurt. "
        },
        new Hinderance
        {
            Id = 10,
            Name = "Cat's Curiosity",
            Description = "Your curiosity will always get the better of you, often leading you to precarious situations. "
        },
        new Hinderance
        {
            Id = 11,
            Name = "Death Wish",
            Description = "Your character isn’t actively trying to kill themself. They’re just more than willing to put themself in harm’s way. "
        },
        new Hinderance
        {
            Id = 12,
            Name = "Illiterate",
            Description = "Can’t read or write. lol"
        },
        new Hinderance
        {
            Id = 13,
            Name = "Near Deaf",
            Description = "You can’t hear shit. Perception checks based on hearing are at disadvantage. "
        },
        new Hinderance
        {
            Id = 14,
            Name = "Dick",
            Description = "You are unreasonobly mean. This has put you in bad situations. But you haven’t stopped. Fuck you. "
        },
        new Hinderance
        {
            Id = 15,
            MindModifier = -2,
            VigorModifier = 0,
            AgilityModifier = 0,
            SpiritModifier = 0,
            SkillModifiers = new List<HinderanceSkillModifier>(),
            Name = "Half-Wit",
            Description = "It’s not your fault.\r\nYour Mind score decreases by 2. "
        },
        new Hinderance
        {
            Id = 16,
            Name = "Greedy",
            Description = "Greedy\r\nYou have made some awful choices, betrayed many who have trusted you, all for your vice of choice. Could be marks, artifacts, fame… "
        },
        new Hinderance
        {
            Id = 17,
            Name = "Overconfidence",
            Description = "You believe you can do anything. This has put you in over your head before. Hasn’t stopped you, though."
        },
        new Hinderance
        {
            Id = 18,
            Name = "Wanted",
            Description = "There’s a warrant out for your arrest. The fuck did you do, bro?"
        },
        new Hinderance
        {
            Id = 19,
            Name = "Coward",
            Description = "When the odds turn against you, you don’t find the courage to endure. "
        },
        new Hinderance
        {
            Id = 20,
            Name = "Code of Honor",
            Description = "You have a strict code that you abide by no matter what. Honor is subjective… "
        },
        new Hinderance
        {
            Id = 21,
            Name = "Kleptomaniac",
            Description = "You steal… compulsively. "
        },
        new Hinderance
        {
            Id = 22,
            Name = "Cursed",
            Description = "This requires some thinking. Maybe your character has been cursed to speak only in the third person."
        },
        new Hinderance
        {
            Id = 23,
            Name = "Restless Legs",
            Description = "You will compulsively move if still for longer than 10 seconds."
        },
        new Hinderance
        {
            Id = 24,
            Name = "Liar",
            Description = "You lie about everything. In fact, you can’t find a way to tell the truth. Help."
        }
    };

    public HinderanceDataService(ICustomHinderanceStorageService customHinderanceStorage)
    {
        _customHinderanceStorage = customHinderanceStorage;
    }

    private async Task EnsureCustomHinderancesLoadedAsync()
    {
        if (_customHinderancesLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_customHinderancesLoaded) return;

            var customHinderances = await _customHinderanceStorage.LoadCustomHinderancesAsync();
            foreach (var hinderance in customHinderances)
            {
                if (!_hinderances.Any(h => h.Id == hinderance.Id))
                {
                    _hinderances.Add(hinderance);
                }
            }
            _customHinderancesLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<List<Hinderance>> GetHinderancesAsync()
    {
        await EnsureCustomHinderancesLoadedAsync();
        return _hinderances;
    }

    public async Task<Hinderance?> GetHinderanceByIdAsync(int id)
    {
        await EnsureCustomHinderancesLoadedAsync();
        return _hinderances.FirstOrDefault(h => h.Id == id);
    }

    public async Task<Hinderance> CreateCustomHinderanceAsync(CreateCustomHinderanceRequest request)
    {
        await EnsureCustomHinderancesLoadedAsync();

        int newId = await _customHinderanceStorage.GetNextCustomHinderanceIdAsync();

        var newHinderance = new Hinderance
        {
            Id = newId,
            Name = request.Name,
            Description = request.Description,
            VigorModifier = request.VigorModifier,
            AgilityModifier = request.AgilityModifier,
            MindModifier = request.MindModifier,
            SpiritModifier = request.SpiritModifier,
            SkillModifiers = request.SkillModifiers ?? new List<HinderanceSkillModifier>()
        };

        _hinderances.Add(newHinderance);
        await _customHinderanceStorage.SaveCustomHinderanceAsync(newHinderance);

        return newHinderance;
    }

    public async Task<bool> UpdateHinderanceAsync(Hinderance hinderance)
    {
        await EnsureCustomHinderancesLoadedAsync();

        var existing = _hinderances.FirstOrDefault(h => h.Id == hinderance.Id);
        if (existing == null) return false;

        var index = _hinderances.IndexOf(existing);
        _hinderances[index] = hinderance;

        if (hinderance.Id >= 70001)
        {
            await _customHinderanceStorage.SaveCustomHinderanceAsync(hinderance);
        }

        return true;
    }

    public async Task<bool> DeleteHinderanceAsync(int id)
    {
        await EnsureCustomHinderancesLoadedAsync();

        var hinderance = _hinderances.FirstOrDefault(h => h.Id == id);
        if (hinderance == null) return false;

        _hinderances.Remove(hinderance);

        if (id >= 70001)
        {
            await _customHinderanceStorage.DeleteCustomHinderanceAsync(id);
        }

        return true;
    }
}