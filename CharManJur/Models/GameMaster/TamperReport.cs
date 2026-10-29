namespace CharManJur.Models;

// Result of comparing a character against the copy saved when the session last closed.
public sealed class TamperReport
{
    public bool HadSnapshot { get; init; }           // false: a character new to this session
    public bool Matches { get; init; }
    public List<string> ChangedSections { get; init; } = new();   // top-level parts of the character that differ
}