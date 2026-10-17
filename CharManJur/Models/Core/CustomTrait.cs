using System;

namespace CharManJur.Models;

// A custom Shifter trait, shareable like any other asset (it travels in Asset Packs).
// PD (Polymorph Dice needed to access it), TraitName and Effect (its description) are all it needs; the
// structured fields inherited from ClassTrait stay empty, so it is descriptive only.
public class CustomTrait : ClassTrait
{
    public Guid Guid { get; set; } = Guid.Empty;   // permanent identity
    public bool IsLoaded { get; set; } = true;
    public string SourcePackId { get; set; } = "Local";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsPlayerCreated { get; set; } = true;
}