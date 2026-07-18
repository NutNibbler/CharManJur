using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class SpellDataService : ISpellDataService
{
    private readonly List<Spell> _spells = new()
    {
        new Spell
        {
            Id = 1,
            SpellName = "Air Bubble",
            SpellDescription = "Creates a bubble of air around the target’s head, allowing them to breathe even in environments that lack oxygen.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 2,
            SpellName = "Alarm",
            SpellDescription = "Ward a single area up to [sum] square feet in size. If any creature enters the area, you instantly know about it. If you so choose, a loud noise can also emanate when the alarm is triggered, heard up to [far]. You can designate up to [dice] creatures who will not set off the alarm. You are automatically excluded.",
            SpellDice = "Hours",
            SpellRange = null
        },
        new Spell
        {
            Id = 3,
            SpellName = "Blink",
            SpellDescription = "Invest 1MD to teleport in a straight line to a point you can see. Invest 3MD to teleport through obstacles.",
            SpellDice = null,
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 4,
            SpellName = "Black Tentacles",
            SpellDescription = "Black tentacles surge from the ground underneath target victim. Victims must pour salt or save VIG to escape, with a penalty equal to [dice], and the tentacles deal 1d6 damage to each victim per round.",
            SpellDice = "Round",
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 5,
            SpellName = "Charm",
            SpellDescription = "The target saves SPI (with penalty equal to [dice]) or regards you as a trusted friend for the duration. They aren’t aware that they’ve been charmed until the spell wears off.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 6,
            SpellName = "Comprehend Language",
            SpellDescription = "You understand, speak, read, & write one language perfectly for the duration. You must have an example of the language present (eg a written sample or a speaker).",
            SpellDice = "Hours",
            SpellRange = "Self"
        },
        new Spell
        {
            Id = 7,
            SpellName = "Command Undead",
            SpellDescription = "Target undead creature obeys your commands for the duration. Intelligent undead get to save, with a penalty equal to [dice].",
            SpellDice = "Hours",
            SpellRange = null
        },
        new Spell
        {
            Id = 8,
            SpellName = "Corpse Lantern",
            SpellDescription = "Touch a corpse of human-size or smaller. It immediately begins to glow with a sickly light, & to float. It will drift after you for the spell’s duration, illuminating an area 10’ x [dice] in diameter.",
            SpellDice = "Hours",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 9,
            SpellName = "Dispel Magic",
            SpellDescription = "Removes one magical effect, with invested [dice] less than or equal to the [dice] invested in this spell.",
            SpellDice = null,
            SpellRange = "30 Feet"
        },
        new Spell
        {
            Id = 10,
            SpellName = "Dragon's Breath",
            SpellDescription = "You can breathe fire for the duration. Each breath of fire deals [sum] fire damage in a 15’ cone. The horribly hot breath deals [dice] fire damage to you each time it is used, if you’re not fire-resistant or some other mitigating factor.",
            SpellDice = "Rounds",
            SpellRange = "Self"
        },
        new Spell
        {
            Id = 11,
            SpellName = "Dream",
            SpellDescription = "Send a dream to any creature you have seen before & can name or clearly visualize (& which is capable of dreaming). The dream lasts up to [sum] minutes.",
            SpellDice = null,
            SpellRange = null
        },
        new Spell
        {
            Id = 12,
            SpellName = "Ear-Piercing Scream",
            SpellDescription = "Creatures in the cone save VIG or are struck deaf for [sum] rounds. Critical failure results in permanent hearing loss.",
            SpellDice = null,
            SpellRange = "30 Foot Cone"
        },
        new Spell
        {
            Id = 13,
            SpellName = "Enlarge",
            SpellDescription = "A single object or person doubles in size. An unwilling creature may save SPI to resist.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 14,
            SpellName = "Explosive Rune",
            SpellDescription = "You write the rune on a surface roughly the size of a regular shield. When a creature steps into the rune’s point of view, they take [sum] fire damage & the rune is destroyed. (when they walk through the area the rune is facing)",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 15,
            SpellName = "Faerie Fire",
            SpellDescription = "Target is outlined in bright light for the duration.",
            SpellDice = "5 Rounds",
            SpellRange = "Far"
        },
        new Spell
        {
            Id = 16,
            SpellName = "Feather Fall",
            SpellDescription = "The target falls slowly & lightly as long as the spell is active. They won’t take any damage from falling.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 17,
            SpellName = "Fireball",
            SpellDescription = "Blast of fire fills a 20’ radius sphere, dealing [sum] fire damage to everything within.",
            SpellDice = null,
            SpellRange = "Far"
        },
        new Spell
        {
            Id = 18,
            SpellName = "Fog Cloud",
            SpellDescription = "Summon a cloud of opaque fog with radius [dice]x10’.",
            SpellDice = null,
            SpellRange = "Far"
        },
        new Spell
        {
            Id = 19,
            SpellName = "Ghostbane Dirge",
            SpellDescription = "A haunting dirge emanates from your body. Corporeal weapons within the radius of the spell affect incorporeal creatures.",
            SpellDice = "Minutes",
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 20,
            SpellName = "Grease",
            SpellDescription = "Summon a blob of slippery, flammable grease. Enough to cover one 5’ square per [dice].",
            SpellDice = null,
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 21,
            SpellName = "Heat Metal",
            SpellDescription = "A metal object becomes unbearably hot. A 1-handed weapon can be affected; a 2-handed weapon with at least 2 dice; a suit of human-sized armour with at least 4 dice. A wearer/wielder takes 1d6 points of damage per round in contact with the object.",
            SpellDice = "1 D6 Rounds",
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 22,
            SpellName = "Inflict Wounds",
            SpellDescription = "Target you touch takes [sum] damage and saves or starts bleeding[dice].",
            SpellDice = null,
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 23,
            SpellName = "Jump",
            SpellDescription = "The target can jump up to [dice] + 1 times their height for the duration.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 24,
            SpellName = "Light",
            SpellDescription = "Object you touch glows with light, illumating an area [dice]x10’ in diameter. If you invest at least 4 dice, the light has all the properties of natural sunlight.",
            SpellDice = "Minutes",
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 25,
            SpellName = "Mage Armour",
            SpellDescription = "A suit of magical force armour surrounds you. 1MD gives 1 armor. 3MD gives 2 armor.",
            SpellDice = "Minutes",
            SpellRange = "Self"
        },
        new Spell
        {
            Id = 26,
            SpellName = "Mage Hands",
            SpellDescription = "You can lift and move objects weighing no more than [dice] x 5 lbs.",
            SpellDice = "Hours",
            SpellRange = "Near"
        },
        new Spell
        {
            Id = 27,
            SpellName = "Mage Circle",
            SpellDescription = "You draw a circle with radius up to [dice]x5’ on the ground. You choose what type of creature the circle is against. You also state whether the circle prevents such creatures from entering or exiting.",
            SpellDice = "Hours",
            SpellRange = null
        },
        new Spell
        {
            Id = 28,
            SpellName = "Raise Skeleton",
            SpellDescription = "Target corpse returns to life as a skeleton with VIG equal to [sum] and HP equal to [dice]. Skeletons can wield weapons.",
            SpellDice = null,
            SpellRange = "Touch"
        },
        new Spell
        {
            Id = 29,
            SpellName = "Telepathic Link",
            SpellDescription = "Link up to [dice] + 1 willing creatures to one another; they can communicate telepathically over any distance, so long as they’re on the same plane. ",
            SpellDice = "Hours",
            SpellRange = null
        },
        new Spell
        {
            Id = 30,
            SpellName = "Water Breathing",
            SpellDescription = "Target can breathe underwater.",
            SpellDice = "Hours",
            SpellRange = "Touch"
        }
    };

    public Task<List<Spell>> GetSpellsAsync()
    {
        return Task.FromResult(_spells);
    }

    public Task<List<Spell>> GetSpellsByFeatureIdAsync(int featureId)
    {
        var result = _spells.Where(s => s.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Spell>> GetSpellsByClassIdAsync(int classId)
    {
        var result = _spells.Where(s => s.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Spell>> GetSpellsForCharacterAsync(int classId, int featureId)
    {
        var result = _spells.Where(s =>
            s.UnlockClassId == classId ||
            s.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<Spell?> GetSpellByIdAsync(int id)
    {
        var result = _spells.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(result);
    }
}