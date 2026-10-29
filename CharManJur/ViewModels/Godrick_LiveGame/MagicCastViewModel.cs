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

// One technique in the cast, with the die that powers it. Each technique gets its own die, and a die powers one technique.
public class TechniqueAssignment : INotifyPropertyChanged
{
    private DieChip? _die;
    private bool _isActive;

    public int Number { get; init; }
    public SubFeatureEntry Entry { get; init; } = null!;

    public string Name => $"{Number}. {Entry.Name}";
    public string Description => Entry.Description;
    public bool HasDescription => Entry.HasDescription;

    public DieChip? Die
    {
        get => _die;
        set
        {
            if (ReferenceEquals(_die, value)) return;
            _die = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDie));
            OnPropertyChanged(nameof(DieText));
        }
    }

    public bool HasDie => _die != null;
    public string DieText => _die == null ? "No die yet" : $"🎲 Die: {_die.Value}";

    // The technique the next tapped die goes to.
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DieChip : INotifyPropertyChanged
{
    private TechniqueAssignment? _owner;

    public int Value { get; init; }
    public bool IsHigh => Value >= 4;                 // 4-6 = fatigue

    // The technique this die powers (null = a spell die: it adds to the sum).
    public TechniqueAssignment? Owner
    {
        get => _owner;
        set
        {
            if (ReferenceEquals(_owner, value)) return;
            _owner = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTechnique));
            OnPropertyChanged(nameof(Badge));
            Changed?.Invoke();
        }
    }

    public bool IsTechnique => _owner != null;
    public string Badge => _owner == null ? string.Empty : $"🌀{_owner.Number}";

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
    private bool _assignedAtRandom;                    // the app chose which dice power the techniques
    private bool _busy;
    private string _manualText = string.Empty;
    private string _combinedName = string.Empty;
    private string _error = string.Empty;

    public ObservableCollection<TechniqueChoice> TechniqueRows { get; } = new();
    public ObservableCollection<DieChip> Dice { get; } = new();
    public ObservableCollection<TechniqueAssignment> AssignmentRows { get; } = new();

    public ICommand DiceUpCommand { get; }
    public ICommand DiceDownCommand { get; }
    public ICommand SetAppRollsCommand { get; }
    public ICommand SetManualCommand { get; }
    public ICommand RollCommand { get; }
    public ICommand UseEnteredCommand { get; }
    public ICommand ToggleDieCommand { get; }
    public ICommand SelectAssignmentCommand { get; }
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
        SelectAssignmentCommand = new Command<TechniqueAssignment>(SelectAssignment);
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

    // Says how the techniques' dice will be decided, before the roll.
    public bool HasRollHint => TechniqueCount > 0;
    public string RollHint => _appRolls
        ? "The app also chooses at random which dice power your techniques. You can't change that."
        : "After you enter your dice, you choose which die powers each technique. Each die can power only one technique.";

    private void RollForPlayer()
    {
        if (PoolTooSmall) { Error = PoolWarning; return; }
        if (_hasRolled) return;
        Lock(MagicDice.Roll(DiceCount), atRandom: true);
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
        Lock(dice, atRandom: false);
    }

    // Once rolled the result is final (no re-rolls). With "App rolls" the app also picks, at random, a different die for
    // each technique. With dice the player typed in, nothing is picked: the player gives each technique its own die.
    private void Lock(List<int> values, bool atRandom)
    {
        _diceCountText = values.Count.ToString();

        Dice.Clear();
        AssignmentRows.Clear();
        foreach (var v in values)
        {
            var chip = new DieChip { Value = v };
            chip.Changed += () => OnPropertyChanged(string.Empty);
            Dice.Add(chip);
        }

        int number = 1;
        foreach (var entry in SelectedTechniques)
            AssignmentRows.Add(new TechniqueAssignment { Number = number++, Entry = entry });

        _hasRolled = true;
        _assignedAtRandom = atRandom && AssignmentRows.Count > 0;

        if (_assignedAtRandom)
        {
            var picks = MagicDice.PickRandomIndices(Dice.Count, AssignmentRows.Count);
            for (int i = 0; i < AssignmentRows.Count && i < picks.Count; i++)
                Give(AssignmentRows[i], Dice[picks[i]]);
        }
        else
        {
            ActivateNext();
        }

        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    // ===== GIVING DICE TO TECHNIQUES (only when the player typed their own dice) =====
    private static void Give(TechniqueAssignment technique, DieChip die)
    {
        technique.Die = die;
        die.Owner = technique;
    }

    private static void Free(TechniqueAssignment technique)
    {
        if (technique.Die == null) return;
        technique.Die.Owner = null;
        technique.Die = null;
    }

    private void SetActive(TechniqueAssignment? active)
    {
        foreach (var row in AssignmentRows) row.IsActive = ReferenceEquals(row, active);
        OnPropertyChanged(string.Empty);
    }

    // The first technique that still needs a die is the one the next tapped die goes to.
    private void ActivateNext()
        => SetActive(AssignmentRows.FirstOrDefault(a => a.Die == null));

    // Tap a die: give it to the active technique, or (if it already powers one) take it back.
    private void ToggleDie(DieChip? chip)
    {
        if (chip == null || !_hasRolled || _assignedAtRandom || AssignmentRows.Count == 0) return;

        if (chip.Owner != null)
        {
            var owner = chip.Owner;
            Free(owner);
            SetActive(owner);
            Error = string.Empty;
            return;
        }

        var target = AssignmentRows.FirstOrDefault(a => a.IsActive && a.Die == null)
                     ?? AssignmentRows.FirstOrDefault(a => a.Die == null);
        if (target == null)
        {
            Error = "Every technique already has a die. Tap a technique, or its die, to free one first.";
            OnPropertyChanged(string.Empty);
            return;
        }

        Give(target, chip);
        Error = string.Empty;
        ActivateNext();
    }

    // Tap a technique: it becomes the one the next die goes to, and gives up the die it had.
    private void SelectAssignment(TechniqueAssignment? row)
    {
        if (row == null || !_hasRolled || _assignedAtRandom) return;
        Free(row);
        Error = string.Empty;
        SetActive(row);
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
                IgnoreDamage = ignoreDamage,
                // Same order as Techniques: the rows were made from the same selection, and every one has a die (CanApply).
                TechniqueDice = AssignmentRows.Where(a => a.Die != null).Select(a => a.Die!.Value).ToList(),
                DiceAssignedAtRandom = _assignedAtRandom
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

    // How many techniques have their die, and whether the player still has to hand out dice.
    public int MarkedCount => AssignmentRows.Count(a => a.Die != null);
    public bool NeedsMarking => _hasRolled && AssignmentRows.Count > 0 && MarkedCount != AssignmentRows.Count;
    public bool HasMarkingText => _hasRolled && AssignmentRows.Count > 0;
    public bool HasAssignments => HasMarkingText;
    public string MarkingText => _assignedAtRandom
        ? "The app assigned a die to each technique at random. Each technique has its own die, and the rest are your spell dice (they make the sum)."
        : $"Give each technique its own die: tap a technique, then tap a die. Tap a die again to take it back. Each die can power one technique, and the rest make the sum. Assigned: {MarkedCount} of {AssignmentRows.Count}.";

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