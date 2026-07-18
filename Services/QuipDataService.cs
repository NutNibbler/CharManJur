using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class QuipDataService : IQuipDataService
{
    private readonly List<Quip> _quips = new()
    {
        new Quip
        {
            Id = 1,
            QuipName = "Book Leaf",
            QuipDescription = "The caster can magically open a book within 10 ft. and leaf through its pages without touching it. "
        },
        new Quip
        {
            Id = 2,
            QuipName = "Cleaning Brush",
            QuipDescription = "A broom or mop takes on a life of its own and cleans an area designated by the caster. "
        },
        new Quip
        {
            Id = 3,
            QuipName = "Coloured Flame",
            QuipDescription = "A flame within range is imbued with a hue of the caster’s choosing. "
        },
        new Quip
        {
            Id = 4,
            QuipName = "Floating Trinket",
            QuipDescription = "The caster causes a small possession of 3lbs or less within range to levitate and float through the air at up to 1’ per round. Can only float, can’t be used. "
        },
        new Quip
        {
            Id = 5,
            QuipName = "Magic Quill",
            QuipDescription = "A quill floats and moves of its own accord, magically transcribing the caster’s words onto a page. "
        },
        new Quip
        {
            Id = 6,
            QuipName = "Open/Close Portal",
            QuipDescription = "An unlocked portal within range swings open or shut as the caster desires. "
        },
        new Quip
        {
            Id = 7,
            QuipName = "Rune",
            QuipDescription = "The caster traces a glowing sigil in the air or on a surface or object. A caster’s rune is unique and can be used by another caster to identify them with context."
        },
        new Quip
        {
            Id = 8,
            QuipName = "Sense Magic",
            QuipDescription = "The caster attunes their mind to subtle arcane energies, attempting to detect magic on a creature or object touched. There is a chance this fails, you will never know… until you do. "
        },
        new Quip
        {
            Id = 9,
            QuipName = "Smoke Rings",
            QuipDescription = "While smoking a pipe, the caster gains the ability to blow impressive smoke rings of any colour desired. "
        },
        new Quip
        {
            Id = 10,
            QuipName = "Spark",
            QuipDescription = "A tiny spark of flame flashes onto the caster’s fingertip. 5’ radius light. Can ignite flammable material. "
        },
        new Quip
        {
            Id = 11,
            QuipName = "Vanish",
            QuipDescription = "A small object touched by the caster becomes invisible for 1 round. No more than 3lbs. "
        }
    };

    public Task<List<Quip>> GetQuipsAsync()
    {
        return Task.FromResult(_quips);
    }

    public Task<List<Quip>> GetQuipsByFeatureIdAsync(int featureId)
    {
        var result = _quips.Where(q => q.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Quip>> GetQuipsByClassIdAsync(int classId)
    {
        var result = _quips.Where(q => q.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Quip>> GetQuipsForCharacterAsync(int classId, int featureId)
    {
        var result = _quips.Where(q =>
            q.UnlockClassId == classId ||
            q.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<Quip?> GetQuipByIdAsync(int id)
    {
        var result = _quips.FirstOrDefault(q => q.Id == id);
        return Task.FromResult(result);
    }
}