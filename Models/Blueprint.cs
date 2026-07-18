using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class Blueprint
{
    public int Id { get; set; }
    public int BlueprintCost { get; set; }
    public int? UnlockFeatureId { get; set; } = 27;
    public int? UnlockClassId { get; set; } = 5;

    public string BlueprintName { get; set; } = string.Empty;
    public string BlueprintDescription { get; set; } = string.Empty;
}
