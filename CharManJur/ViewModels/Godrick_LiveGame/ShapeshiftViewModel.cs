using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class TraitPick : INotifyPropertyChanged
{
    // Index order matches DamageElement (Ice = 1 ... Lightning = 6).
    private static readonly string[] ElementList = { "Ice", "Fire", "Acid", "Wind", "Earth", "Lightning" };

    private bool _isSelected;
    private int _elementIndex = -1;

    public ClassTrait Trait { get; init; } = null!;
    public string Name => Trait.TraitName;
    public string Effect => Trait.Effect;
    public string PdText => Trait is CustomTrait ? $"PD {Trait.PD} · custom" : $"PD {Trait.PD}";

    // A trait that lets the player choose its element shows a picker once it is ticked.
    public IReadOnlyList<string> ElementNames => ElementList;
    public bool ShowElementPicker => Trait.ChooseElement && _isSelected;

    public int ElementIndex
    {
        get => _elementIndex;
        set
        {
            if (_elementIndex == value) return;
            _elementIndex = value;
            OnPropertyChanged();
            ElementChanged?.Invoke();
        }
    }

    public DamageElement SelectedElement =>
        _elementIndex >= 0 && _elementIndex < ElementList.Length ? (DamageElement)(_elementIndex + 1) : DamageElement.None;

    public void SetElement(DamageElement element) => ElementIndex = element == DamageElement.None ? -1 : (int)element - 1;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowElementPicker));
            SelectionChanged?.Invoke();
        }
    }

    public event Action? SelectionChanged;
    public event Action? ElementChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class SavedFormRow
{
    public SavedForm Form { get; init; } = null!;
    public string Name => Form.Name;
    public string Summary { get; init; } = string.Empty;
    public ICommand UseCommand { get; init; } = null!;
    public ICommand DeleteCommand { get; init; } = null!;
}

// The transform screen: roll Polymorph Dice for Stability, pick traits (and elements), name and describe the form, transform.
public class ShapeshiftViewModel : INotifyPropertyChanged
{
    private readonly LiveShifter _shifter;
    private readonly CharacterClass _class;
    private readonly Action<string> _onDone;
    private readonly Func<Task> _close;
    private readonly int _pool;

    private List<ClassTrait> _custom = new();
    private bool _loaded;
    private int _dice = 1;
    private bool _appRolls = true;
    private string _manualText = string.Empty;
    private List<int>? _rolled;
    private FormPreview _preview = new();
    private string _vigorText = "10";
    private string _agilityText = "10";
    private string _formName = string.Empty;
    private string _formDescription = string.Empty;
    private bool _saveForm = true;
    private SavedForm? _pendingForm;    // a saved form picked before rolling: applied once the traits are known
    private SavedForm? _appliedForm;    // its scores were already approved
    private bool _applying;
    private IReadOnlyList<SavedFormRow> _savedForms = Array.Empty<SavedFormRow>();
    private string _error = string.Empty;
    private bool _busy;

    public ObservableCollection<DieChip> Dice { get; } = new();
    public ObservableCollection<TraitPick> Traits { get; } = new();
    public IReadOnlyList<SavedFormRow> SavedForms => _savedForms;

    public ICommand DiceUpCommand { get; }
    public ICommand DiceDownCommand { get; }
    public ICommand SetAppRollsCommand { get; }
    public ICommand SetManualCommand { get; }
    public ICommand RollCommand { get; }
    public ICommand UseEnteredCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenTraitCreatorCommand { get; }

    public ShapeshiftViewModel(LiveShifter shifter, CharacterClass shifterClass, Action<string> onDone, Func<Task> close)
    {
        _shifter = shifter;
        _class = shifterClass;
        _onDone = onDone;
        _close = close;
        _pool = shifter.PoolSize;

        DiceUpCommand = new Command(() => SetDice(_dice + 1));
        DiceDownCommand = new Command(() => SetDice(_dice - 1));
        SetAppRollsCommand = new Command(() => SetMode(true));
        SetManualCommand = new Command(() => SetMode(false));
        RollCommand = new Command(RollForPlayer);
        UseEnteredCommand = new Command(UseEntered);
        ConfirmCommand = new Command(async () => await ConfirmAsync());
        CancelCommand = new Command(async () => await _close());
        OpenTraitCreatorCommand = new Command(async () => await OpenTraitCreatorAsync());
    }

    // Loads the custom traits and the saved forms (once).
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        _custom = await _shifter.CustomTraitsAsync();
        RebuildSaved();
    }

    // ===== SAVED FORMS =====
    private void RebuildSaved()
    {
        _savedForms = _shifter.SavedForms
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => new SavedFormRow
            {
                Form = f,
                Summary = $"{f.PolymorphDice} {(f.PolymorphDice == 1 ? "die" : "dice")} · {f.Traits.Count} traits"
                          + (f.Traits.Count > 0 ? $" ({string.Join(", ", f.Traits.Take(3))}{(f.Traits.Count > 3 ? "…" : "")})" : ""),
                UseCommand = new Command(() => UseSaved(f)),
                DeleteCommand = new Command(async () => await DeleteSavedAsync(f))
            })
            .ToList();
        OnPropertyChanged(string.Empty);
    }

    public bool HasSavedForms => _savedForms.Count > 0;

    private void UseSaved(SavedForm form)
    {
        if (PoolShort) return;

        if (_rolled == null)
        {
            // Before rolling: set the dice count and remember the form. Its traits are ticked after the roll.
            _dice = Math.Clamp(form.PolymorphDice, 1, _pool);
            _pendingForm = form;
            _formName = form.Name;
            _formDescription = form.Description;
            OnPropertyChanged(string.Empty);
        }
        else
        {
            ApplyForm(form);
        }
    }

    private async Task DeleteSavedAsync(SavedForm form)
    {
        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Saved Form", $"Delete the saved form \"{form.Name}\"?", "Delete", "Cancel");
        if (!ok) return;

        await _shifter.DeleteSavedFormAsync(form);
        if (ReferenceEquals(_pendingForm, form)) _pendingForm = null;
        RebuildSaved();
    }

    // Ticks the form's traits (the ones reachable with the dice rolled), restores its element picks
    // and fills in its scores.
    private void ApplyForm(SavedForm form)
    {
        _applying = true;
        try
        {
            var names = new HashSet<string>(form.Traits, StringComparer.OrdinalIgnoreCase);
            foreach (var t in Traits)
            {
                t.IsSelected = names.Contains(t.Name);
                if (form.Elements != null && form.Elements.TryGetValue(t.Name, out var element))
                    t.SetElement(element);
            }
        }
        finally { _applying = false; }

        _formName = form.Name;
        _formDescription = form.Description;

        Recompute();
        _vigorText = form.Vigor.ToString();
        _agilityText = form.Agility.ToString();
        _appliedForm = form;   // its scores were approved when it was saved

        int missing = form.Traits.Count(n => !Traits.Any(t => string.Equals(t.Name, n, StringComparison.OrdinalIgnoreCase)));
        Error = missing > 0 ? $"{missing} of this form's traits aren't available with the dice you rolled." : string.Empty;
        OnPropertyChanged(string.Empty);
    }

    // ===== DICE =====
    public bool PoolShort => _pool < 1;
    public string DiceText => _dice.ToString();
    public string DiceRangeText => $"Invest 1 to {_pool}. More dice mean more traits and more Stability, but every die showing 4-6 adds 1 fatigue.";

    private void SetDice(int value)
    {
        if (_rolled != null || PoolShort) return;
        _dice = Math.Clamp(value, 1, _pool);   // the pool is a hard cap
        OnPropertyChanged(string.Empty);
    }

    public bool ShowRollControls => !PoolShort && _rolled == null;
    public bool HasRolled => _rolled != null;
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

    // Once rolled the result is final, and the traits within reach appear (foundation and custom).
    private void Lock(List<int> values)
    {
        _rolled = values;

        Dice.Clear();
        foreach (var v in values) Dice.Add(new DieChip { Value = v });

        Traits.Clear();
        foreach (var t in LiveShifter.TraitsFor(_class, values.Count, _custom))
        {
            var pick = new TraitPick { Trait = t };
            pick.SelectionChanged += () => { if (!_applying) Recompute(); };
            pick.ElementChanged += () => OnPropertyChanged(nameof(FormText));   // an element doesn't change the scores
            Traits.Add(pick);
        }

        if (_pendingForm != null)
        {
            var form = _pendingForm;
            _pendingForm = null;
            ApplyForm(form);
        }
        else
        {
            Recompute();
        }
    }

    // ===== CUSTOM TRAIT CREATOR =====
    private async Task OpenTraitCreatorAsync()
    {
        var page = Application.Current?.MainPage;
        if (page == null) return;

        var traitService = IPlatformApplication.Current!.Services.GetRequiredService<ITraitDataService>();
        var creator = new CustomTraitCreatorPage(traitService, async created => await AddNewCustomTraitsAsync());
        await page.Navigation.PushModalAsync(creator);
    }

    // After a trait is created: the new one joins the list. Nothing already ticked, picked or typed changes.
    private async Task AddNewCustomTraitsAsync()
    {
        _custom = await _shifter.CustomTraitsAsync();
        if (_rolled == null) return;   // not rolled yet: the list builds from _custom when the dice lock in

        var existing = Traits.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<TraitPick>();
        foreach (var t in LiveShifter.TraitsFor(_class, _rolled.Count, _custom))
        {
            if (!existing.TryGetValue(t.TraitName, out var pick))
            {
                pick = new TraitPick { Trait = t };
                pick.SelectionChanged += () => { if (!_applying) Recompute(); };
                pick.ElementChanged += () => OnPropertyChanged(nameof(FormText));
            }
            ordered.Add(pick);
        }

        Traits.Clear();
        foreach (var pick in ordered) Traits.Add(pick);
    }

    // ===== TRAITS AND FORM =====
    private List<ClassTrait> Picked => Traits.Where(t => t.IsSelected).Select(t => t.Trait).ToList();

    // The element picked for each ticked trait that lets the player choose one.
    private Dictionary<string, DamageElement> PickElements()
        => Traits.Where(t => t.IsSelected && t.Trait.ChooseElement && t.SelectedElement != DamageElement.None)
                 .ToDictionary(t => t.Name, t => t.SelectedElement);

    private void Recompute()
    {
        _appliedForm = null;   // different traits: the saved scores no longer apply
        _preview = LiveShifter.Preview(Picked, _rolled?.Count ?? _dice);
        _vigorText = _preview.Vigor.ToString();
        _agilityText = _preview.Agility.ToString();
        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    public string ResultText => _rolled == null ? string.Empty
        : $"Stability: {_rolled.Sum()}     Fatigue: +{_rolled.Count(d => d >= 4)}     (dice showing 4-6)";

    public string TraitsHeader => _rolled == null ? string.Empty
        : $"Traits: {Picked.Count} of {LiveShifter.TraitsAllowed(_rolled.Count)}  (from the lists up to PD {Math.Min(_rolled.Count, 4)})";

    public string VigorText
    {
        get => _vigorText;
        set => _vigorText = value ?? string.Empty;
    }

    public string AgilityText
    {
        get => _agilityText;
        set => _agilityText = value ?? string.Empty;
    }

    // ===== NAME, DESCRIPTION, SAVING =====
    public string FormName
    {
        get => _formName;
        set
        {
            _formName = value ?? string.Empty;
            OnPropertyChanged(nameof(HasOverwriteHint));
        }
    }

    public string FormDescription
    {
        get => _formDescription;
        set => _formDescription = value ?? string.Empty;
    }

    public bool SaveForm
    {
        get => _saveForm;
        set
        {
            _saveForm = value;
            OnPropertyChanged(nameof(SaveForm));
            OnPropertyChanged(nameof(HasOverwriteHint));
        }
    }

    private string EffectiveName => string.IsNullOrWhiteSpace(_formName) ? LiveShifter.GeneratedName(Picked) : _formName.Trim();

    public string NamePlaceholder => $"Name (or leave blank: {LiveShifter.GeneratedName(Picked)})";

    public bool HasOverwriteHint => _saveForm && _rolled != null
        && _shifter.SavedForms.Any(f => string.Equals(f.Name, EffectiveName, StringComparison.OrdinalIgnoreCase));
    public string OverwriteHint => $"A saved form named \"{EffectiveName}\" already exists. Saving will replace it.";

    // Armor, duration and a line per natural weapon, so the player sees what the form brings.
    public string FormText
    {
        get
        {
            var lines = new List<string>();

            var parts = new List<string>();
            if (_preview.Armor > 0)
                parts.Add(string.IsNullOrEmpty(_preview.ArmorName) ? $"Armor {_preview.Armor}" : $"{_preview.ArmorName}: Armor {_preview.Armor}");
            parts.Add($"Lasts {LiveShifter.DurationText(_preview.DurationMinutes)}");
            lines.Add(string.Join(" · ", parts));

            foreach (var pick in Traits.Where(t => t.IsSelected && t.Trait.WeaponDie.HasValue))
            {
                var weapon = LiveShifter.BuildWeapons(new[] { pick.Trait }, PickElements()).First();
                string line = $"{weapon.Name}: {LiveShifter.WeaponSummary(weapon)}";
                string effects = LiveShifter.WeaponEffectsText(weapon);
                if (effects.Length > 0) line += $" ({effects})";
                if (pick.Trait.ChooseElement && weapon.Element == DamageElement.None) line += " · choose an element";
                lines.Add(line);
            }

            return string.Join("\n", lines);
        }
    }

    public bool HasClash => _preview.VigorClash || _preview.AgilityClash;
    public string ClashText
    {
        get
        {
            var lines = new List<string>();
            if (_preview.VigorClash) lines.Add($"Your traits disagree about VIG, so the middle value ({_preview.Vigor}) is proposed.");
            if (_preview.AgilityClash) lines.Add($"Your traits disagree about AGI, so the middle value ({_preview.Agility}) is proposed.");
            if (lines.Count > 0) lines.Add("You can change it below. Your GM should approve a different value.");
            return string.Join("\n", lines);
        }
    }

    public bool HasWarnings => _preview.Warnings.Count > 0;
    public string WarningsText => HasWarnings
        ? string.Join("\n", _preview.Warnings.Select(w => "⚠ " + w)) + "\nYou can still transform. It will be flagged in your Event Tray."
        : string.Empty;

    // ===== CONFIRM =====
    public bool CanConfirm => _rolled != null && !_busy;
    public string ConfirmText => HasWarnings ? "✅ Transform anyway" : "✅ Transform";

    private async Task ConfirmAsync()
    {
        if (_busy || _rolled == null) return;

        // A trait that lets you choose its element needs one.
        var missing = Traits.FirstOrDefault(t => t.IsSelected && t.Trait.ChooseElement && t.SelectedElement == DamageElement.None);
        if (missing != null)
        {
            Error = $"Choose an element for {missing.Name}.";
            return;
        }

        if (!int.TryParse(_vigorText.Trim(), out int vigor) || !int.TryParse(_agilityText.Trim(), out int agility)
            || vigor < 1 || vigor > 30 || agility < 1 || agility > 30)
        {
            Error = "VIG and AGI must be whole numbers from 1 to 30.";
            return;
        }

        var page = Application.Current!.MainPage!;
        bool changed = vigor != _preview.Vigor || agility != _preview.Agility;
        bool approved = _appliedForm != null && vigor == _appliedForm.Vigor && agility == _appliedForm.Agility;
        if (changed && !approved)
        {
            var changes = new List<string>();
            if (vigor != _preview.Vigor) changes.Add($"VIG {_preview.Vigor} → {vigor}");
            if (agility != _preview.Agility) changes.Add($"AGI {_preview.Agility} → {agility}");

            bool ok = await page.DisplayAlertAsync("Change Form Scores",
                $"You changed the proposed scores: {string.Join(", ", changes)}.\n\nYour GM should approve this. Continue?",
                "Continue", "Cancel");
            if (!ok) return;
        }

        _busy = true;
        OnPropertyChanged(nameof(CanConfirm));
        try
        {
            var result = await _shifter.TransformAsync(new TransformRequest
            {
                Rolled = _rolled,
                Traits = Picked,
                Vigor = vigor,
                Agility = agility,
                Name = EffectiveName,
                Description = _formDescription.Trim(),
                SaveForm = _saveForm,
                Elements = PickElements()
            });

            if (!result.Success)
            {
                Error = result.Message;
                return;
            }

            _onDone(result.Message);
            await _close();
        }
        finally
        {
            _busy = false;
            OnPropertyChanged(nameof(CanConfirm));
        }
    }

    // ===== DISPLAY =====
    public string Title => "🐺 Shapeshift";
    public string Subtitle => $"Polymorph Dice: {_pool}";
    public string PoolShortText => "You have no Polymorph Dice, so you can't transform.";

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