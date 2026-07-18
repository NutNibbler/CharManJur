using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class BlueprintDataService : IBlueprintDataService
{
    private readonly List<Blueprint> _blueprints = new()
    {
        new Blueprint
        {
            Id = 1,
            BlueprintName = "Tool",
            BlueprintDescription = "Held. Things like swords, shovels, drills, etc. ",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 2,
            BlueprintName = "Garment",
            BlueprintDescription = "Worn, gives passive bonuses. Armor, cloaks, etc.",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 3,
            BlueprintName = "Vessel",
            BlueprintDescription = "A container of something. Potions, munitions. Single use.",
            BlueprintCost = 2
        },
        new Blueprint
        {
            Id = 4,
            BlueprintName = "Gauge",
            BlueprintDescription = "Measures something. Hourglass, telescope, etc. ",
            BlueprintCost = 4
        },
        new Blueprint
        {
            Id = 5,
            BlueprintName = "Launcher",
            BlueprintDescription = "Fires something at range. Gun, catapolt, etc. ",
            BlueprintCost = 5
        },
        new Blueprint
        {
            Id = 6,
            BlueprintName = "Apparatus",
            BlueprintDescription = "Exists in place. Can be a work bench, shelter, printer, etc.",
            BlueprintCost = 5
        },
        new Blueprint
        {
            Id = 7,
            BlueprintName = "Transport",
            BlueprintDescription = "Moves under its own power, directly controlled. ",
            BlueprintCost = 3
        },
        new Blueprint
        {
            Id = 8,
            BlueprintName = "Prosthesis",
            BlueprintDescription = "Fused with a living creature taking the place of an organ/extremity. ",
            BlueprintCost = 3
        }
    };

    public Task<List<Blueprint>> GetBlueprintsAsync()
    {
        return Task.FromResult(_blueprints);
    }

    public Task<List<Blueprint>> GetBlueprintsByFeatureIdAsync(int featureId)
    {
        var result = _blueprints.Where(b => b.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Blueprint>> GetBlueprintsByClassIdAsync(int classId)
    {
        var result = _blueprints.Where(b => b.UnlockClassId == classId).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Blueprint>> GetBlueprintsForCharacterAsync(int classId, int featureId)
    {
        var result = _blueprints.Where(b => 
            b.UnlockClassId == classId || 
            b.UnlockFeatureId == featureId).ToList();
        return Task.FromResult(result);
    }

    public Task<Blueprint?> GetBlueprintByIdAsync(int id)
    {
        var result = _blueprints.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(result);
    }
}