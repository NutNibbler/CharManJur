using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class SkillRowDisplay
{
    public string Name { get; set; } = string.Empty;
    public string NaturalText { get; set; } = string.Empty;
    public string AsmText { get; set; } = string.Empty;
    public string AsmMaxText { get; set; } = string.Empty;
    public string TrainingText { get; set; } = string.Empty;
    public string ModText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public bool ShowAsmMax { get; set; }
    public bool AsmReduced { get; set; }
    public bool AsmRaised { get; set; }
    public bool TotalReduced { get; set; }
    public bool TotalRaised { get; set; }
    public double NaturalOpacity { get; set; } = 1;
    public double TrainingOpacity { get; set; } = 1;
    public double ModOpacity { get; set; } = 1;

    // Selection: the chosen row is enlarged and, while points are available, shows − and + together.
    public bool IsSelected { get; set; }
    public bool ShowTrainingControls { get; set; }
    public bool ShowTapHint { get; set; }   // points are available and this row isn't the chosen one: it looks tappable
    public Thickness RowPadding { get; set; } = new(4, 1);
    public double NameSize { get; set; } = 12;
    public double NumberSize { get; set; } = 11;
    public double TotalSize { get; set; } = 12;
    public string DraftText { get; set; } = string.Empty;

    public bool CanUp { get; set; }
    public bool CanDown { get; set; }
    public ICommand SelectCommand { get; set; } = null!;
    public ICommand UpCommand { get; set; } = null!;
    public ICommand DownCommand { get; set; } = null!;
}

public class StatGroupDisplay
{
    public string Name { get; set; } = string.Empty;
    public string ValueText { get; set; } = string.Empty;
    public Color GroupColor { get; set; } = Colors.Gray;
    public bool ValueReduced { get; set; }
    public bool ValueRaised { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public bool HasNote => NoteText.Length > 0;
    public List<SkillRowDisplay> Rows { get; set; } = new();
}

// The Bio Sheet's skills table and training: points are spent into a draft, and Finish Training makes it permanent.
public class BioSheetViewModel : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _svc;
    private readonly TrainingManager _training;
    private readonly Dictionary<string, int> _draft = new();
    private string? _selected;

    // Visual grouping per the rulebook's skill table, not used for any calculation.
    private static readonly (string Stat, string[] Skills)[] StatGroups =
    {
        ("Vigor", new[] { "Athletics", "Constitution", "Grapple", "Presence", "Ride" }),
        ("Agility", new[] { "Acrobatics", "Aim", "Drive", "Stealth", "Thief" }),
        ("Mind", new[] { "Arcana", "Artifice", "Heal", "Investigate", "Lore" }),
        ("Spirit", new[] { "Commune", "Deception", "Diplomacy", "Sight", "Survival" }),
    };

    public ObservableCollection<StatGroupDisplay> Groups { get; } = new();

    public ICommand BestowCommand { get; }
    public ICommand FinishCommand { get; }
    public ICommand ResetCommand { get; }

    public BioSheetViewModel(ICharAttribDataService svc, TrainingManager training)
    {
        _svc = svc;
        _training = training;

        BestowCommand = new Command(async () => await BestowAsync());
        FinishCommand = new Command(async () => await FinishAsync());
        ResetCommand = new Command(() => { _draft.Clear(); Refresh(); });
    }

    // ===== TRAINING STATE =====
    public int DraftTotal => _draft.Values.Sum();
    public bool HasDraft => DraftTotal > 0;
    public int Remaining => _training.Unspent - DraftTotal;
    public bool HasPoints => _training.Unspent > 0;

    public string PointsText => HasDraft
        ? $"Training points: {Remaining} left · {DraftTotal} allocated (not yet final)"
        : $"Training points: {Remaining} available";

    public string StudyReminder =>
        "Tap a skill, then use + / − to spend points, and press Finish Training. Studying takes about 6 weeks, plus 1 week for each level of training the skill already has (a reminder, not enforced).";

    // ===== BUILD =====
    public void Refresh()
    {
        Groups.Clear();

        foreach (var (stat, skills) in StatGroups)
        {
            int max = MaxOf(stat), current = CurrentOf(stat);
            int maxAsm = MaxAsmOf(stat), currentAsm = CurrentAsmOf(stat);

            var group = new StatGroupDisplay
            {
                Name = stat,
                GroupColor = FixedColor($"{stat}Color", Colors.Gray),
                ValueText = current == max
                    ? $"{max} (ASM {Signed(currentAsm)})"
                    : $"{current} / {max} (ASM {Signed(currentAsm)}, max {Signed(maxAsm)})",
                ValueReduced = current < max,
                ValueRaised = current > max
            };

            // Explain a reduced (or raised) stat in plain words.
            if (current != max)
            {
                int diff = Math.Abs(max - current);
                string direction = current < max ? "below" : "above";
                group.NoteText = currentAsm != maxAsm
                    ? $"{stat} is {diff} {direction} its max, so these skills use ASM {Signed(currentAsm)} instead of {Signed(maxAsm)}."
                    : $"{stat} is {diff} {direction} its max, but the modifier is unchanged.";
            }

            foreach (var skill in skills) group.Rows.Add(BuildRow(skill));
            Groups.Add(group);
        }

        OnPropertyChanged(string.Empty);
    }

    private SkillRowDisplay BuildRow(string skill)
    {
        var breakdown = _svc.GetSkillBreakdown(skill);
        int maxAsm = _svc.GetSkillMaxASM(skill);
        int draft = _draft.GetValueOrDefault(skill);
        bool selected = _selected == skill;

        // The total is red/green when effects or a reduced (or raised) stat change it from the baseline.
        int dir = Math.Sign(_svc.SkillDirection(skill) + Math.Sign(breakdown.ASM - maxAsm));

        return new SkillRowDisplay
        {
            Name = skill,
            NaturalText = Signed(breakdown.NaturalNegative),
            NaturalOpacity = breakdown.NaturalNegative == 0 ? 0.45 : 1,
            AsmText = Signed(breakdown.ASM),
            AsmMaxText = $"max {Signed(maxAsm)}",
            ShowAsmMax = breakdown.ASM != maxAsm,
            AsmReduced = breakdown.ASM < maxAsm,
            AsmRaised = breakdown.ASM > maxAsm,
            TrainingText = draft > 0 ? $"{Signed(breakdown.TrainingPoints)} +{draft}" : Signed(breakdown.TrainingPoints),
            TrainingOpacity = breakdown.TrainingPoints == 0 && draft == 0 ? 0.45 : 1,
            ModText = Signed(breakdown.OtherModifiers),
            ModOpacity = breakdown.OtherModifiers == 0 ? 0.45 : 1,
            TotalText = Signed(breakdown.Total + draft),
            TotalReduced = dir < 0,
            TotalRaised = dir > 0,

            IsSelected = selected,
            ShowTrainingControls = selected && HasPoints,
            ShowTapHint = HasPoints && !selected,
            RowPadding = selected ? new Thickness(4, 6) : new Thickness(4, 1),
            NameSize = selected ? 14 : 12,
            NumberSize = selected ? 12 : 11,
            TotalSize = selected ? 14 : 12,
            DraftText = draft > 0 ? $"+{draft} allocated" : "none allocated",

            CanUp = Remaining > 0,
            CanDown = draft > 0,
            SelectCommand = new Command(() => Select(skill)),
            UpCommand = new Command(() => Allocate(skill, +1)),
            DownCommand = new Command(() => Allocate(skill, -1))
        };
    }

    private void Select(string skill)
    {
        _selected = _selected == skill ? null : skill;
        Refresh();
    }

    private void Allocate(string skill, int delta)
    {
        int current = _draft.GetValueOrDefault(skill);

        if (delta > 0 && Remaining <= 0) return;
        if (delta < 0 && current <= 0) return;

        int next = current + delta;
        if (next <= 0) _draft.Remove(skill); else _draft[skill] = next;
        Refresh();
    }

    // ===== ACTIONS =====
    private async Task BestowAsync()
    {
        var page = Application.Current!.MainPage!;

        string? answer = await page.DisplayPromptAsync(
            "Bestow Training Points",
            "How many training points? A GM normally awards these, so giving them to yourself is flagged as rule skating in your Event Tray.",
            "Bestow", "Cancel", initialValue: "1", keyboard: Keyboard.Numeric);
        if (answer == null) return;

        if (!int.TryParse(answer.Trim(), out int points) || points < 1)
        {
            await page.DisplayAlertAsync("Invalid Amount", "Enter a whole number of 1 or more.", "OK");
            return;
        }

        await _training.BestowAsync(points);
        Refresh();
    }

    private async Task FinishAsync()
    {
        if (!HasDraft) return;
        var page = Application.Current!.MainPage!;

        bool ok = await page.DisplayAlertAsync("Finish Training",
            $"Spend {DraftTotal} training point(s) on the skills you chose? Training is permanent.", "Finish Training", "Cancel");
        if (!ok) return;

        await CommitDraftAsync();
        await page.DisplayAlertAsync("Training Complete", "Your training has been applied.", "OK");
    }

    // Also used when the sheet is closed with a pending allocation.
    public async Task<bool> CommitDraftAsync()
    {
        if (!HasDraft) return true;

        bool ok = await _training.FinishAsync(_draft);
        _draft.Clear();
        Refresh();
        return ok;
    }

    // ===== STAT LOOKUPS =====
    private int MaxOf(string stat) => stat switch
    {
        "Vigor" => _svc.TotalStatVigor,
        "Agility" => _svc.TotalStatAgility,
        "Mind" => _svc.TotalStatMind,
        _ => _svc.TotalStatSpirit
    };

    private int CurrentOf(string stat) => stat switch
    {
        "Vigor" => _svc.CurrentStatVigor,
        "Agility" => _svc.CurrentStatAgility,
        "Mind" => _svc.CurrentStatMind,
        _ => _svc.CurrentStatSpirit
    };

    private int MaxAsmOf(string stat) => stat switch
    {
        "Vigor" => _svc.TotalASMStatVigor,
        "Agility" => _svc.TotalASMStatAgility,
        "Mind" => _svc.TotalASMStatMind,
        _ => _svc.TotalASMStatSpirit
    };

    private int CurrentAsmOf(string stat) => stat switch
    {
        "Vigor" => _svc.CurrentASMStatVigor,
        "Agility" => _svc.CurrentASMStatAgility,
        "Mind" => _svc.CurrentASMStatMind,
        _ => _svc.CurrentASMStatSpirit
    };

    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private static Color FixedColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}