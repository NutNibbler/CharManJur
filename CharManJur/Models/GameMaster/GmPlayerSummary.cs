namespace CharManJur.Models;

// One stat on a player's card: "VIG 12/14". State 0 is normal, 1 is below its max, 2 is at 0 or overflowing.
public sealed class GmStatChip
{
    public GmStatChip(string label, string value, int state = 0)
    {
        Label = label;
        Value = value;
        IsReduced = state == 1;
        IsCritical = state >= 2;
    }

    public string Label { get; }
    public string Value { get; }
    public bool IsReduced { get; }
    public bool IsCritical { get; }
}

// What the GM sees of a connected player at a glance, read the way the live game would show it.
public sealed class GmPlayerSummary
{
    public List<GmStatChip> Chips { get; init; } = new();
    public string Compact { get; init; } = "HP ? · VIG ?";    // the one-line version for the headline strip
    public int Waiting { get; init; }                         // damage and effects sent but not yet taken on
}