using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class FeaturePanelViewModel : INotifyPropertyChanged
{
    private readonly LiveFeatures _live;
    private readonly FavourTracker _favour;
    private FeatureEntry? _selected;
    private string _resultText = string.Empty;
    private bool _rebuilding;
    private bool _busy;

    public ObservableCollection<FeatureEntry> Rows { get; } = new();

    public ICommand UseCommand { get; }
    public ICommand PassDayCommand { get; }
    public ICommand FavourUpCommand { get; }
    public ICommand FavourDownCommand { get; }

    // A feature with a roll or a chosen cost opens the use page (the home page builds it).
    public event Action<FeatureEntry>? UseRequested;
    // The dashboard should reload (fatigue, chips, Spirit, Favour...).
    public event Action? CharacterChanged;

    public FeaturePanelViewModel(LiveFeatures live, FavourTracker favour)
    {
        _live = live;
        _favour = favour;

        UseCommand = new Command(async () => await UseAsync());
        PassDayCommand = new Command(async () => await PassDayAsync());
        FavourUpCommand = new Command(() => _favour.Adjust(+1));
        FavourDownCommand = new Command(() => _favour.Adjust(-1));

        _favour.Changed += () => OnPropertyChanged(string.Empty);
    }

    public async Task RefreshAsync()
    {
        var keep = _selected;
        var owned = await _live.OwnedAsync();

        _rebuilding = true;
        try
        {
            Rows.Clear();
            foreach (var entry in owned) Rows.Add(entry);
            _selected = keep == null ? null : Rows.FirstOrDefault(r => r.Id == keep.Id);
        }
        finally { _rebuilding = false; }

        OnPropertyChanged(string.Empty);
    }

    public void ShowResult(string text)
    {
        _resultText = text;
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(HasResult));
    }

    public FeatureEntry? SelectedRow
    {
        get => _selected;
        set
        {
            if (_rebuilding || ReferenceEquals(_selected, value)) return;
            _selected = value;
            _resultText = string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }

    // ===== USE =====
    private async Task UseAsync()
    {
        if (_busy || _selected == null || _selected.IsPassive) return;
        var entry = _selected;

        if (entry.NeedsInput)
        {
            UseRequested?.Invoke(entry);
            return;
        }

        // No choices: a plain log, with a confirm only when there is something to warn about or spend.
        var warnings = _live.Check(entry, 0);
        bool needsConfirm = warnings.Count > 0 || entry.Behavior.Costs.Count > 0
                            || entry.Behavior.Conditions.Any(c => c.Check == ConditionCheck.None);
        if (needsConfirm)
        {
            var page = Application.Current!.MainPage!;
            bool ok = await page.DisplayAlertAsync(entry.Name, _live.BuildConfirmText(entry, warnings),
                warnings.Count > 0 ? "Use anyway" : "Use", "Cancel");
            if (!ok) return;
        }

        _busy = true;
        try
        {
            var result = await _live.UseAsync(entry, new FeatureUseInput());
            _resultText = result.Message;
            await RefreshAsync();
            CharacterChanged?.Invoke();
        }
        finally { _busy = false; }
    }

    private async Task PassDayAsync()
    {
        if (_busy) return;
        var page = Application.Current!.MainPage!;
        bool ok = await page.DisplayAlertAsync("Pass a Day",
            "Let a day pass? Features limited to once per day become available again.", "Pass a day", "Cancel");
        if (!ok) return;

        _busy = true;
        try
        {
            var result = await _live.PassDayAsync();
            _resultText = result.Message;
            await RefreshAsync();
        }
        finally { _busy = false; }
    }

    // ===== DISPLAY =====
    public bool HasFavour => _live.UsesFavour;
    public string FavourText => _favour.Value.ToString();

    public string PoolsText
    {
        get
        {
            var parts = new List<string>();
            foreach (DicePool pool in Enum.GetValues<DicePool>())
            {
                int size = _live.PoolSize(pool);
                if (size > 0) parts.Add($"{DicePools.Name(pool)}: {size}");
            }
            return string.Join("  ·  ", parts);
        }
    }
    public bool HasPools => PoolsText.Length > 0;
    public string DaysText => $"Days passed: {_live.DaysPassed}";

    public bool HasNoRows => Rows.Count == 0;
    public bool HasSelection => _selected != null;
    public bool HasNoSelection => _selected == null;

    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedClassName => _selected?.ClassName ?? string.Empty;
    public string SelectedDescription => _selected?.Description ?? string.Empty;
    public string SelectedStatus => _selected?.StatusText ?? string.Empty;
    public string RulesText => _selected == null ? string.Empty : string.Join("\n", _live.RulesLines(_selected));

    public bool CanUse => _selected is { IsPassive: false };
    public bool IsPassiveSelected => _selected is { IsPassive: true };
    public string UseText => _selected switch
    {
        { IsShapeshift: true } => _live.IsTransformed ? "🐺 Manage form…" : "🐺 Transform…",
        { NeedsInput: true } => "⚙️ Use…",
        _ => "⚙️ Use"
    };

    public string ResultText => _resultText;
    public bool HasResult => _resultText.Length > 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}