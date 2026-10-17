namespace CharManJur.Models;

public enum LongRestType
{
    None,
    Perilous,
    Normal,
    Comfortable
}

public class LongRestResult
{
    public LongRestType Type { get; init; }
    public bool FatigueCleared { get; init; }

    // Result of a six-sided die roll, applied to all four ability scores. Null unless Comfortable.
    public int? AbilityScoreRestoreRoll { get; init; }
}