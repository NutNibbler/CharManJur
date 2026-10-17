using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class TechniqueChoice : INotifyPropertyChanged
{
    private bool _isSelected;

    public SubFeatureEntry Entry { get; init; } = null!;
    public bool IsLocked { get; init; }               // the technique being used: always part of the combination
    public string Name => Entry.Name;
    public string Description => Entry.Description;
    public bool HasDescription => Entry.HasDescription;
    public bool CanToggle => !IsLocked;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            SelectionChanged?.Invoke();
        }
    }

    public event Action? SelectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DieChip : INotifyPropertyChanged
{
    private bool _isTechnique;

    public int Value { get; init; }
    public bool IsHigh => Value >= 4;                 // 4-6 = fatigue

    public bool IsTechnique
    {
        get => _isTechnique;
        set
        {
            if (_isTechnique == value) return;
            _isTechnique = value;
            OnPropertyChanged();
            Changed?.Invoke();
        }
    }

    public event Action? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// The roll screen for casting a Spell (optionally with Techniques) or sculpting a cantrip from Techniques.
public class MagicCastViewModel : INotifyPropertyChanged
{
    private readonly MagicCasting _casting;
    private readonly SubFeatureEntry? _spell;          // null = sculpting a cantrip from techniques
    private readonly Action<string> _onDone;
    private readonly Func<Task> _close;

    private string _diceCountText = "1";
    private bool _appRolls = true;
    private bool _hasRolled;
    private bool _busy;
    private string _manualText = string.Empty;
    private string _combinedName = string.Empty;
    private string _error = string.Empty;

    public ObservableCollection<TechniqueChoice> TechniqueRows { get; } = new();
    public ObservableCollection<DieChip> Dice { get; } = new();

    public ICommand DiceUpCommand { get; }
    public ICommand DiceDownCommand { get; }
    public ICommand SetAppRollsCommand { get; }
    public ICommand SetManualCommand { get; }
    public ICommand RollCommand { get; }
    public ICommand UseEnteredCommand { get; }
    public ICommand ToggleDieCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand IgnoreDamageCommand { get; }
    public ICommand CancelCommand { get; }

    public MagicCastViewModel(
        MagicCasting casting,
        SubFeatureEntry? spell,
        SubFeatureEntry? technique,
        IEnumerable<SubFeatureEntry> ownedTechniques,
        Action<string> onDone,
        Func<Task> close)
    {
        _casting = casting;
        _spell = spell;
        _onDone = onDone;
        _close = close;

        foreach (var t in ownedTechniques.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            bool locked = technique != null && t.IsSameAs(technique);
            var choice = new TechniqueChoice { Entry = t, IsLocked = locked };
            choice.IsSelected = locked;
            choice.SelectionChanged += OnTechniqueSelectionChanged;
            TechniqueRows.Add(choice);
        }
        _diceCountText = MinDice.ToString();

        DiceUpCommand = new Command(() => SetDiceCount(DiceCount + 1));
        DiceDownCommand = new Command(() => SetDiceCount(DiceCount - 1));
        SetAppRollsCommand = new Command(() => SetMode(true));
        SetManualCommand = new Command(() => SetMode(false));
        RollCommand = new Command(RollForPlayer);
        UseEnteredCommand = new Command(UseEntered);
        ToggleDieCommand = new Command<DieChip>(ToggleDie);
        ApplyCommand = new Command(async () => await ApplyAsync(false));
        IgnoreDamageCommand = new Command(async () => await ApplyAsync(true));
        CancelCommand = new Command(async () => await _close());
    }

    // ===== TECHNIQUES AND DICE COUNT =====
    private bool IsSpellMode => _spell != null;
    private List<SubFeatureEntry> SelectedTechniques => TechniqueRows.Where(t => t.IsSelected).Select(t => t.Entry).ToList();
    public int TechniqueCount => TechniqueRows.Count(t => t.IsSelected);
    public int MinDice => 1 + TechniqueCount;                       // the spell or bolt itself, plus 1 per technique
                                                                    // The Magic Dice pool is a hard cap on how many can be invested.
    private int PoolMax => _casting.MagicDicePool;
    public bool PoolTooSmall => PoolMax < MinDice;
    public string PoolWarning => PoolTooSmall
        ? $"You have {PoolMax} Magic {(PoolMax == 1 ? "Die" : "Dice")}, but this needs at least {MinDice}. Remove a technique, or gain more Magic Dice."
        : string.Empty;
    private int DiceCount => Math.Clamp(int.TryParse(_diceCountText, out int n) ? n : MinDice, MinDice, Math.Max(MinDice, PoolMax));

    private void OnTechniqueSelectionChanged()
    {
        _diceCountText = DiceCount.ToString();   // never below the new minimum
        OnPropertyChanged(string.Empty);
    }

    private void SetDiceCount(int count)
    {
        _diceCountText = Math.Clamp(count, MinDice, Math.Max(MinDice, PoolMax)).ToString();
        OnPropertyChanged(string.Empty);
    }

    public string DiceCountText
    {
        get => _diceCountText;
        set => _diceCountText = value ?? string.Empty;   // no refresh while typing; it is clamped when used
    }

    public string MinDiceText => $"(at least {MinDice}, your pool is {PoolMax})";

    // ===== ROLLING =====
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
        if (PoolTooSmall) { Error = PoolWarning; return; }
        if (_hasRolled) return;
        Lock(MagicDice.Roll(DiceCount));
    }

    private void UseEntered()
    {
        if (PoolTooSmall) { Error = PoolWarning; return; }
        if (_hasRolled) return;
        if (!MagicDice.TryParse(_manualText, DiceCount, out var dice, out string error))
        {
            Error = error;
            return;
        }
        Lock(dice);
    }

    // Once rolled the result is final (no re-rolls). The lowest dice are pre-marked as technique dice,
    // matching the rulebook example; the player can tap dice to change that.
    private void Lock(List<int> values)
    {
        _diceCountText = values.Count.ToString();
        Dice.Clear();
        foreach (var v in values)
        {
            var chip = new DieChip { Value = v };
            chip.Changed += () => OnPropertyChanged(string.Empty);
            Dice.Add(chip);
        }
        _hasRolled = true;

        foreach (var chip in Dice.OrderBy(d => d.Value).Take(TechniqueCount)) chip.IsTechnique = true;

        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    private void ToggleDie(DieChip? chip)
    {
        if (chip == null || !_hasRolled || TechniqueCount == 0) return;
        chip.IsTechnique = !chip.IsTechnique;
    }

    private MagicRollResult? CurrentRoll => !_hasRolled ? null
        : MagicDice.Evaluate(Dice.Select(d => d.Value).ToList(), Dice.Select(d => d.IsTechnique).ToList());

    // ===== APPLY =====
    private async Task ApplyAsync(bool ignoreDamage)
    {
        if (_busy || !CanApply) return;
        var roll = CurrentRoll;
        if (roll == null) return;

        _busy = true;
        try
        {
            var result = await _casting.ApplyCastAsync(new CastRequest
            {
                Spell = _spell,
                Techniques = SelectedTechniques,
                CombinedName = !IsSpellMode && !string.IsNullOrWhiteSpace(_combinedName) ? _combinedName.Trim() : null,
                Roll = roll,
                IgnoreDamage = ignoreDamage
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
    public string Title => _spell != null ? $"📜 Cast {_spell.Name}" : "🌀 Sculpt a Cantrip";
    public string Subtitle => _spell != null
        ? "Choose how many Magic Dice to invest, then roll."
        : "Combine techniques into a cantrip. Each technique adds 1 Magic Die to the roll.";

    public bool IsTechniqueMode => !IsSpellMode;
    public bool HasDescription => IsSpellMode && _spell!.HasDescription;
    public string ResolvedDescription
    {
        get
        {
            if (_spell == null) return string.Empty;
            var roll = CurrentRoll;
            return roll == null ? _spell.Description : MagicDice.FillPlaceholders(_spell.Description, roll.DiceCount, roll.Sum);
        }
    }

    public bool HasTechniques => TechniqueRows.Count > 0;
    public string TechniqueHeader => IsSpellMode
        ? "Techniques to apply (only if your GM agreed they can be applied to this spell, +1 Magic Die each)"
        : "Techniques to combine (+1 Magic Die each)";

    public string CombinedName
    {
        get => _combinedName;
        set => _combinedName = value ?? string.Empty;
    }
    public string CombinedNamePlaceholder
    {
        get
        {
            string generic = string.Join(" + ", SelectedTechniques.Select(t => t.Name));
            return generic.Length > 0 ? $"Name this technique (or leave blank: {generic})" : "Name this technique";
        }
    }

    public bool HasRolled => _hasRolled;
    public bool NotRolled => !_hasRolled;

    public int MarkedCount => Dice.Count(d => d.IsTechnique);
    public bool NeedsMarking => _hasRolled && TechniqueCount > 0 && MarkedCount != TechniqueCount;
    public bool HasMarkingText => _hasRolled && TechniqueCount > 0;
    public string MarkingText => $"Tap dice to choose which {TechniqueCount} power your techniques (they add nothing to the sum). " +
                                 $"Marked: {MarkedCount} of {TechniqueCount}.";

    public string DiceLine
    {
        get
        {
            var r = CurrentRoll;
            if (r == null) return string.Empty;
            return IsSpellMode
                ? $"[dice] = {r.DiceCount}     [sum] = {r.Sum}"
                : $"Damage = {r.Sum} + {r.DiceCount} dice = {r.CantripDamage}";
        }
    }

    public string FatigueLine
    {
        get
        {
            var r = CurrentRoll;
            if (r == null) return string.Empty;
            return r.Fatigue > 0 ? $"Fatigue: +{r.Fatigue} (dice showing 4-6)" : "Fatigue: none";
        }
    }

    public bool HasSeries => CurrentRoll?.HasSeries ?? false;
    public string SeriesLine
    {
        get
        {
            var r = CurrentRoll;
            if (r == null || !r.HasSeries) return string.Empty;
            return $"⚠ SERIES ({r.SeriesText}): {r.SeriesDamage} damage to you{(r.SeriesIsBlast ? ", with the blast property" : "")}. " +
                   "It hits your Hit Protection first; any excess goes to your Mind.";
        }
    }

    public string Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => _error.Length > 0;

    public bool CanApply => _hasRolled && !NeedsMarking;
    public string ApplyText => IsSpellMode ? "✅ Cast" : "✅ Sculpt";

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}