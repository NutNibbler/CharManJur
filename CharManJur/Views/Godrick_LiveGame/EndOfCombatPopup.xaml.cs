using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CharManJur.Views.Godrick_LiveGame;

public class UsageDieRow : INotifyPropertyChanged
{
    private bool _isUsed;
    private bool _manualMode;

    public int ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Sides { get; init; }

    public string DieLabel => UsageDie.Label(Sides);
    public string Hint => $"1-{Sides}";
    public string ManualText { get; set; } = string.Empty;

    public bool IsUsed
    {
        get => _isUsed;
        set { _isUsed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowManual)); }
    }

    public bool ManualMode
    {
        get => _manualMode;
        set { _manualMode = value; OnPropertyChanged(nameof(ShowManual)); }
    }

    public bool ShowManual => IsUsed && ManualMode;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class EndOfCombatPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onResolved;

    public ObservableCollection<UsageDieRow> Rows { get; } = new();
    public bool HasRows => Rows.Count > 0;
    public bool HasNoRows => Rows.Count == 0;

    public EndOfCombatPopup(ICharAttribDataService charDataService, Action onResolved)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onResolved = onResolved;
        BindingContext = this;

        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        foreach (var item in _charDataService.GetUsageDieItems().OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            Rows.Add(new UsageDieRow
            {
                ItemId = item.Id,
                Name = item.DisplayName,
                Sides = item.UsageDieSides,
                ManualMode = ManualRadio.IsChecked
            });
        }
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(HasNoRows));
    }

    private void OnModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        foreach (var row in Rows) row.ManualMode = ManualRadio.IsChecked;
    }

    private async void OnResolveClicked(object sender, EventArgs e)
    {
        var used = Rows.Where(r => r.IsUsed).ToList();
        if (used.Count == 0)
        {
            await DisplayAlertAsync("Nothing Selected", "Tick the items you used this fight.", "OK");
            return;
        }

        // Work out every roll first, so a bad manual entry stops everything before anything changes.
        var rolls = new List<(UsageDieRow Row, int Roll)>();
        foreach (var row in used)
        {
            if (ManualRadio.IsChecked)
            {
                if (!int.TryParse(row.ManualText, out int roll) || roll < 1 || roll > row.Sides)
                {
                    await DisplayAlertAsync("Invalid Roll", $"Enter the result of your {row.DieLabel} for {row.Name} (1 to {row.Sides}).", "OK");
                    return;
                }
                rolls.Add((row, roll));
            }
            else
            {
                rolls.Add((row, Random.Shared.Next(1, row.Sides + 1)));
            }
        }

        var lines = new List<string>();
        foreach (var (row, roll) in rolls)
        {
            var outcome = _charDataService.ResolveUsageDieRoll(row.ItemId, roll);
            if (outcome != null) lines.Add(outcome.Summary);
        }

        _onResolved();
        await DisplayAlertAsync("End of Combat", string.Join("\n", lines), "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();
}