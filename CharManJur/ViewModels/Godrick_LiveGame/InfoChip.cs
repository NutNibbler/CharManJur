using System.Windows.Input;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// A small label on a wielded weapon. A tappable one opens a popup explaining it.
public class InfoChip
{
    public string Text { get; init; } = string.Empty;
    public bool IsTappable { get; init; }
    public bool IsEffect { get; set; }          // an effect or skill (colored differently from damage information)
    public ICommand? TapCommand { get; init; }

    public static InfoChip Plain(string text) => new() { Text = text };

    public static InfoChip Tappable(string text, string title, string body) => new()
    {
        Text = text,
        IsTappable = true,
        TapCommand = new Command(async () =>
            await Application.Current!.MainPage!.DisplayAlertAsync(title, body, "OK"))
    };
}

// One row of chips on a wielded weapon.
public class ChipLine
{
    public List<InfoChip> Chips { get; init; } = new();
}

public static class ChipLayout
{
    // About how many characters fit on one row of the equipment column. Lower it if rows are clipped,
    // raise it if there is spare room.
    public const int LineBudget = 56;

    // Fills chips into rows left to right, starting a new row when the next chip wouldn't fit.
    public static List<ChipLine> Split(IEnumerable<InfoChip> chips, int budget = LineBudget)
    {
        var lines = new List<ChipLine>();
        var current = new List<InfoChip>();
        int used = 0;

        foreach (var chip in chips)
        {
            int cost = chip.Text.Length + 2;   // the gap after each chip
            if (current.Count > 0 && used + cost > budget)
            {
                lines.Add(new ChipLine { Chips = current });
                current = new List<InfoChip>();
                used = 0;
            }
            current.Add(chip);
            used += cost;
        }

        if (current.Count > 0) lines.Add(new ChipLine { Chips = current });
        return lines;
    }
}