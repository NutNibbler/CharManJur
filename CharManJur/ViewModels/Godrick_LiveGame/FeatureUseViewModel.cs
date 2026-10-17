using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// The use screen for a feature with a roll or a cost the player chooses (Favour).
public class FeatureUseViewModel : INotifyPropertyChanged
{
    private const int MaxFavour = 99;

    private readonly LiveFeatures _live;
    private readonly FeatureEntry _entry;
    private readonly Action<string> _onDone;
    private readonly Func<Task> _close;
    private readonly FeatureRoll? _roll;
    private readonly FeatureCost? _favourCost;   // the Favour cost the player chooses, if any
    private readonly int _pool;

    private int _dice;
    private int _favour;
    private bool _appRolls = true;
    private string _manualText = string.Empty;
    private List<int>? _rolled;
    private string _error = string.Empty;
    private bool _busy;

    public ObservableCollection<DieChip> Dice { get; } = new();

    public ICommand DiceUpCommand { get; }
    public ICommand DiceDownCommand { get; }
    public ICommand FavourUpCommand { get; }
    public ICommand FavourDownCommand { get; }
    public ICommand SetAppRollsCommand { get; }
    public ICommand SetManualCommand { get; }
    public ICommand RollCommand { get; }
    public ICommand UseEnteredCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public FeatureUseViewModel(LiveFeatures live, FeatureEntry entry, Action<string> onDone, Func<Task> close)
    {
        _live = live;
        _entry = entry;
        _onDone = onDone;
        _close = close;

        _roll = entry.Behavior.Roll;
        _favourCost = entry.Behavior.Costs.FirstOrDefault(c => c.Type == FeatureCostType.Favour && c.Variable);
        _pool = _roll != null ? live.PoolSize(_roll.Pool) : 0;
        _dice = DiceMin;
        _favour = _favourCost?.Amount ?? 0;

        DiceUpCommand = new Command(() => SetDice(_dice + 1));
        DiceDownCommand = new Command(() => SetDice(_dice - 1));
        FavourUpCommand = new Command(() => SetFavour(_favour + 1));
        FavourDownCommand = new Command(() => SetFavour(_favour - 1));
        SetAppRollsCommand = new Command(() => SetMode(true));
        SetManualCommand = new Command(() => SetMode(false));
        RollCommand = new Command(RollForPlayer);
        UseEnteredCommand = new Command(UseEntered);
        ConfirmCommand = new Command(async () => await ConfirmAsync());
        CancelCommand = new Command(async () => await _close());
    }

    // ===== DICE AND FAVOUR =====
    private int DiceMin => _roll?.MinDice ?? 0;
    private int DiceMax => _roll == null ? 0 : (_roll.Variable ? Math.Max(DiceMin, _pool) : DiceMin);   // the pool is a hard cap

    private void SetDice(int value)
    {
        if (_rolled != null) return;
        _dice = Math.Clamp(value, DiceMin, DiceMax);
        OnPropertyChanged(string.Empty);
    }

    private void SetFavour(int value)
    {
        if (_rolled != null || _favourCost == null) return;
        _favour = Math.Clamp(value, _favourCost.Amount, MaxFavour);
        OnPropertyChanged(string.Empty);
    }

    public bool HasVariableDice => _roll is { Variable: true };
    public string DiceText => _dice.ToString();
    public string DiceRangeText => _roll == null ? string.Empty : $"{DicePools.Name(_roll.Pool)}: your pool is {_pool}";

    public bool HasVariableFavour => _favourCost != null;
    public string FavourText => _favour.ToString();
    public string FavourHaveText => $"You have {_live.FavourAvailable} Favour";

    public bool PoolShort => _roll != null && _pool < _roll.MinDice;
    public string PoolShortText => _roll == null ? string.Empty
        : $"You have {_pool} {DicePools.Name(_roll.Pool)}, but this needs at least {_roll.MinDice}.";

    // ===== ROLLING =====
    public bool NeedsRoll => _roll != null && _dice > 0;
    public bool HasRolled => _rolled != null;
    public bool NotRolled => _rolled == null;
    public bool ShowRollControls => NeedsRoll && !PoolShort && _rolled == null;
    public bool IsAppRolls => _appRolls;
    public bool IsManual => !_appRolls;

    private void SetMode(bool appRolls)
    {
        _appRolls = appRolls;
        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    public string ManualText
    {
        get => _manualText;
        set => _manualText = value ?? string.Empty;
    }

    private void RollForPlayer()
    {
        if (!ShowRollControls) return;
        Lock(MagicDice.Roll(_dice));
    }

    private void UseEntered()
    {
        if (!ShowRollControls) return;
        if (!MagicDice.TryParse(_manualText, _dice, out var dice, out string error))
        {
            Error = error;
            return;
        }
        Lock(dice);
    }

    // Once rolled the result is final.
    private void Lock(List<int> values)
    {
        _rolled = values;
        Dice.Clear();
        foreach (var v in values) Dice.Add(new DieChip { Value = v });
        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    // ===== CONFIRM =====
    private bool InvestmentOk => !_entry.Behavior.RequiresInvestment || _dice > 0 || _favour > 0;
    public bool CanConfirm => !PoolShort && (!NeedsRoll || _rolled != null) && InvestmentOk;

    private async Task ConfirmAsync()
    {
        if (_busy || !CanConfirm) return;
        _busy = true;
        try
        {
            var result = await _live.UseAsync(_entry, new FeatureUseInput
            {
                Dice = _rolled ?? new List<int>(),
                Favour = _favour
            });

            if (!result.Success)
            {
                Error = result.Message;
                return;
            }

            _onDone(result.Message);
            await _close();
        }
        finally { _busy = false; }
    }

    // ===== DISPLAY =====
    public string Title => $"⚙️ {_entry.Name}";
    public string Subtitle => _entry.ClassName;
    public string Description => _entry.Description;
    public string RulesText => string.Join("\n", _live.RulesLines(_entry));

    public IReadOnlyList<FeatureWarning> Warnings => _live.Check(_entry, _favour);
    public bool HasWarnings => Warnings.Count > 0;
    public string WarningText => string.Join("\n", Warnings.Select(w => "⚠ " + w.Text))
        + (HasWarnings ? "\nYou can still use it. Your GM may rule otherwise, and it will be flagged in your Event Tray." : string.Empty);

    public string StatusText
    {
        get
        {
            if (PoolShort) return PoolShortText;
            if (!InvestmentOk) return "Invest at least one die or one Favour.";
            if (NeedsRoll && _rolled == null) return "Roll the dice to continue.";
            return string.Empty;
        }
    }
    public bool HasStatus => StatusText.Length > 0;

    public string ResultText
    {
        get
        {
            if (_rolled == null || _roll == null) return string.Empty;
            int high = _rolled.Count(d => d >= 4);
            int low = _rolled.Count - high;
            int fatigue = high * _roll.FatiguePerHighDie;
            int spirit = low * _roll.SpiritLossPerLowDie + high * _roll.SpiritLossPerHighDie;

            var lines = new List<string> { $"Dice: {_rolled.Count}    Sum: {_rolled.Sum()}    (1-3: {low}, 4-6: {high})" };
            if (fatigue > 0) lines.Add($"Fatigue +{fatigue}");
            if (spirit > 0) lines.Add($"Spirit -{spirit}");
            return string.Join("\n", lines);
        }
    }

    public string PreviewText => LiveFeatures.Fill(_entry.Behavior.UseMessage, _rolled ?? new List<int>(), _favour);
    public bool HasPreview => HasRolled && PreviewText.Length > 0;

    public string ConfirmText => HasWarnings ? "✅ Use anyway" : "✅ Use";

    public string Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => _error.Length > 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}