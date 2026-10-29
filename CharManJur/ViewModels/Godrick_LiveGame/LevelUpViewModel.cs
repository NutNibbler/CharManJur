using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// One tappable choice in a single-choice list (a class, a feature, a weapon, a quip...).
public class PickOption : INotifyPropertyChanged
{
    private bool _selected;

    public object Tag { get; init; } = null!;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string SearchText { get; init; } = string.Empty;
    public bool HasSubtitle => Subtitle.Length > 0;
    public bool HasDetail => Detail.Length > 0;
    public string Mark => IsSelected ? "◉" : "○";
    public ICommand SelectCommand { get; set; } = null!;

    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Mark));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class AbilityRow
{
    public string Name { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
    public string AddedText { get; set; } = string.Empty;
    public bool CanUp { get; set; }
    public bool CanDown { get; set; }
    public ICommand UpCommand { get; set; } = null!;
    public ICommand DownCommand { get; set; } = null!;
}

// The Level Up page: collects every choice, then hands them to LevelUpService in one request.
public class LevelUpViewModel : INotifyPropertyChanged
{
    private readonly LevelUpService _svc;
    private readonly ICharAttribDataService _data;
    private readonly IItemDataService _items;
    private readonly Dictionary<string, int> _alloc = LevelUpRules.Stats.ToDictionary(s => s, _ => 0);

    private CharacterClass? _class;
    private ClassFeature? _feature;
    private Item? _weapon;
    private SubFeatureEntry? _quip;
    private SubFeatureEntry? _subFeature;
    private List<PickOption> _weaponAll = new();
    private string _weaponSearch = string.Empty;
    private string _constructName = string.Empty;
    private string _constructDescription = string.Empty;
    private string _constructCapacity = "0";
    private int _loadToken;
    private bool _busy;

    public ObservableCollection<AbilityRow> AbilityRows { get; } = new();
    public ObservableCollection<PickOption> ClassOptions { get; } = new();
    public ObservableCollection<PickOption> FeatureOptions { get; } = new();
    public ObservableCollection<PickOption> SubFeatureOptions { get; } = new();
    public ObservableCollection<PickOption> WeaponOptions { get; } = new();
    public ObservableCollection<PickOption> QuipOptions { get; } = new();

    public ICommand ConfirmCommand { get; }
    public ICommand CreateSubFeatureCommand { get; }
    public ICommand CreateQuipCommand { get; }

    // Raised after the level was applied: the page closes itself.
    public event Action? Finished;

    public LevelUpViewModel(LevelUpService svc, ICharAttribDataService data, IItemDataService items)
    {
        _svc = svc;
        _data = data;
        _items = items;

        ConfirmCommand = new Command(async () => await ConfirmAsync());
        CreateSubFeatureCommand = new Command(async () =>
        {
            if (_feature is { UnlockableTypes.Count: > 0 })
                await CreateCustomAsync(_feature.UnlockableTypes[0], AddedSubFeature);
        });
        CreateQuipCommand = new Command(async () => await CreateCustomAsync(SubFeatureType.Quip, AddedQuip));
    }

    public async Task InitializeAsync()
    {
        RebuildAbilities();
        await ReloadClassesAsync();
        Refresh();
    }

    // ===== HEADER =====
    public string HeaderText => $"Level {_data.CharacterLevel} → {_data.CharacterLevel + 1}";
    public string PendingText => SessionMode.GmHosted ? $"{_data.PendingLevels} level-up(s) granted by your GM" : string.Empty;
    public bool HasPendingText => PendingText.Length > 0;
    public string RuleSkatingNote => SessionMode.GmHosted
        ? string.Empty
        : "No GM awards levels in single-player, so this level-up is flagged as rule skating in your Event Tray.";
    public bool HasRuleSkatingNote => RuleSkatingNote.Length > 0;

    // ===== 1. ABILITY POINTS =====
    public int PointsLeft => LevelUpRules.AbilityPointsPerLevel - _alloc.Values.Sum();
    public bool AllocationComplete => PointsLeft == 0;
    public string PointsText => AllocationComplete
        ? "✅ Both ability points placed"
        : $"{PointsLeft} of {LevelUpRules.AbilityPointsPerLevel} ability points left to place";

    private int CurrentScore(string stat) => stat switch
    {
        "Vigor" => _data.TotalStatVigor,
        "Agility" => _data.TotalStatAgility,
        "Mind" => _data.TotalStatMind,
        _ => _data.TotalStatSpirit
    };

    private void RebuildAbilities()
    {
        AbilityRows.Clear();
        foreach (var stat in LevelUpRules.Stats)
        {
            string s = stat;
            int added = _alloc[stat];
            int current = CurrentScore(stat);
            AbilityRows.Add(new AbilityRow
            {
                Name = stat,
                ScoreText = added > 0 ? $"{current} → {current + added}" : current.ToString(),
                AddedText = added > 0 ? $"+{added}" : string.Empty,
                CanUp = PointsLeft > 0,
                CanDown = added > 0,
                UpCommand = new Command(async () => await AllocateAsync(s, +1)),
                DownCommand = new Command(async () => await AllocateAsync(s, -1))
            });
        }
    }

    private async Task AllocateAsync(string stat, int delta)
    {
        if (delta > 0 && PointsLeft <= 0) return;
        if (delta < 0 && _alloc[stat] <= 0) return;

        _alloc[stat] += delta;
        RebuildAbilities();
        await ReloadClassesAsync();
        Refresh();
    }

    // ===== 2. CLASS =====
    public bool ShowClassSection => AllocationComplete;

    private async Task ReloadClassesAsync()
    {
        ClassOptions.Clear();
        if (!AllocationComplete)
        {
            if (_class != null) await SetClassAsync(null);
            return;
        }

        var allowed = (await _svc.AllowedClassesAsync(_alloc))
            .OrderBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var c in allowed)
        {
            int picks = _svc.ClassPicks(c.Id);
            var option = Option(c.ClassName,
                $"Needs: {c.LevelingSkillBonus}" + (picks > 0 ? $" · picked ×{picks}" : string.Empty),
                c.RecurringBenefit, c, OnClassPicked);
            option.IsSelected = _class?.Id == c.Id;
            ClassOptions.Add(option);
        }

        if (_class != null && allowed.All(c => c.Id != _class.Id)) await SetClassAsync(null);   // the points no longer allow it
    }

    private void OnClassPicked(PickOption option)
    {
        var cls = (CharacterClass)option.Tag;
        if (_class?.Id == cls.Id) return;
        _ = SetClassAsync(cls);
    }

    private async Task SetClassAsync(CharacterClass? cls)
    {
        int token = ++_loadToken;

        _class = cls;
        _feature = null;
        _subFeature = null;
        _weapon = null;
        _quip = null;
        _weaponSearch = string.Empty;

        foreach (var o in ClassOptions) o.IsSelected = cls != null && ((CharacterClass)o.Tag).Id == cls.Id;
        FeatureOptions.Clear();
        SubFeatureOptions.Clear();
        WeaponOptions.Clear();
        QuipOptions.Clear();
        _weaponAll = new List<PickOption>();

        if (cls != null)
        {
            foreach (var f in _svc.AvailableFeatures(cls))
            {
                string hint = f.UnlockableTypes.Count > 0
                    ? $"You also choose a {LiveSubFeatures.TypeName(f.UnlockableTypes[0]).ToLowerInvariant()}."
                    : string.Empty;
                FeatureOptions.Add(Option(f.Name, hint, f.Description ?? string.Empty, f, OnFeaturePicked));
            }

            if (cls.RecurringChoice == ClassChoice.Weapon)
            {
                var choices = await WeaponChoices.LoadAsync(_items, _data.Proficiency.GrantedSkills);
                if (token != _loadToken) return;
                _weaponAll = choices
                    .Select(c => Option(c.Name, c.Summary, c.SkillsText, c.Item, OnWeaponPicked, c.SearchText))
                    .ToList();
                ApplyWeaponFilter();
            }
            else if (cls.RecurringChoice == ClassChoice.Quip)
            {
                var quips = await _svc.AvailableSubFeaturesAsync(SubFeatureType.Quip);
                if (token != _loadToken) return;
                foreach (var q in quips) QuipOptions.Add(SubOption(q, OnQuipPicked));
            }
        }

        Refresh();
    }

    // ===== 3. FEATURE =====
    public bool ShowFeatureSection => _class != null && _class.Features.Count > 0;
    public bool NoFeaturesLeft => ShowFeatureSection && FeatureOptions.Count == 0;
    public string ClassNameText => _class?.ClassName ?? string.Empty;

    private void OnFeaturePicked(PickOption option)
    {
        Mark(FeatureOptions, option);
        _feature = (ClassFeature)option.Tag;
        _subFeature = null;
        SubFeatureOptions.Clear();

        if (_feature.UnlockableTypes.Count > 0) _ = LoadSubFeaturesAsync(_feature.UnlockableTypes[0]);
        Refresh();
    }

    private async Task LoadSubFeaturesAsync(SubFeatureType type)
    {
        int token = ++_loadToken;
        var list = await _svc.AvailableSubFeaturesAsync(type);
        if (token != _loadToken) return;

        SubFeatureOptions.Clear();
        foreach (var e in list) SubFeatureOptions.Add(SubOption(e, OnSubFeaturePicked));
        Refresh();
    }

    // ===== 3b. THE SPELL / TECHNIQUE / BLUEPRINT A FEATURE ADDS =====
    public bool ShowSubFeatureSection => _feature is { UnlockableTypes.Count: > 0 };
    private string SubTypeName => _feature is { UnlockableTypes.Count: > 0 }
        ? LiveSubFeatures.TypeName(_feature.UnlockableTypes[0]) : string.Empty;
    public string SubFeatureHeader => $"Choose a {SubTypeName.ToLowerInvariant()} for {_feature?.Name}";
    public string CreateSubFeatureText => $"✨ Create Custom {SubTypeName}";

    private void OnSubFeaturePicked(PickOption option)
    {
        Mark(SubFeatureOptions, option);
        _subFeature = (SubFeatureEntry)option.Tag;
        Refresh();
    }

    private void AddedSubFeature(SubFeatureEntry entry)
    {
        var option = SubOption(entry, OnSubFeaturePicked);
        SubFeatureOptions.Insert(0, option);
        OnSubFeaturePicked(option);
    }

    // ===== 4a. WARRIOR WEAPON =====
    public bool ShowWeaponSection => _class?.RecurringChoice == ClassChoice.Weapon;
    public string WeaponSelectionText => _weapon == null ? "Choose a weapon to continue." : $"Chosen: {_weapon.Name}";

    public string WeaponSearch
    {
        get => _weaponSearch;
        set
        {
            if (_weaponSearch == value) return;
            _weaponSearch = value ?? string.Empty;
            ApplyWeaponFilter();
            OnPropertyChanged();
        }
    }

    private void ApplyWeaponFilter()
    {
        string term = _weaponSearch.Trim();
        var shown = term.Length == 0
            ? _weaponAll
            : _weaponAll.Where(o => o.SearchText.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        WeaponOptions.Clear();
        foreach (var o in shown) WeaponOptions.Add(o);
    }

    private void OnWeaponPicked(PickOption option)
    {
        Mark(_weaponAll, option);
        _weapon = (Item)option.Tag;
        Refresh();
    }

    // ===== 4b. ARCANIST QUIP =====
    public bool ShowQuipSection => _class?.RecurringChoice == ClassChoice.Quip;
    public string QuipSelectionText => _quip == null ? "Choose a quip to continue." : $"Chosen: {_quip.Name}";

    private void OnQuipPicked(PickOption option)
    {
        Mark(QuipOptions, option);
        _quip = (SubFeatureEntry)option.Tag;
        Refresh();
    }

    private void AddedQuip(SubFeatureEntry entry)
    {
        var option = SubOption(entry, OnQuipPicked);
        QuipOptions.Insert(0, option);
        OnQuipPicked(option);
    }

    // ===== 4c. ARTIFICER CONSTRUCT =====
    public bool ShowConstructSection => _class?.RecurringChoice == ClassChoice.Construct;

    public string ConstructName
    {
        get => _constructName;
        set { _constructName = value ?? string.Empty; OnPropertyChanged(); RefreshSummary(); }
    }

    public string ConstructDescription
    {
        get => _constructDescription;
        set
        {
            _constructDescription = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConstructWordsText));
            OnPropertyChanged(nameof(ConstructWordsOver));
            RefreshSummary();
        }
    }

    public string ConstructCapacityText
    {
        get => _constructCapacity;
        set { _constructCapacity = value ?? string.Empty; OnPropertyChanged(); RefreshSummary(); }
    }

    public string ConstructWordsText =>
        $"{LevelUpRules.WordCount(_constructDescription)} / {LevelUpRules.ConstructDescriptionWords} words";
    public bool ConstructWordsOver => LevelUpRules.WordCount(_constructDescription) > LevelUpRules.ConstructDescriptionWords;

    // ===== 5. SUMMARY AND CONFIRM =====
    public bool ShowSummary => _class != null;

    public string WarningText => _class == null ? string.Empty : string.Join("\n", _svc.SoftLimitWarnings(_class).Select(w => "⚠️ " + w));
    public bool HasWarnings => WarningText.Length > 0;

    public string SummaryText
    {
        get
        {
            if (_class == null) return string.Empty;

            int picks = _svc.ClassPicks(_class.Id) + 1;
            var lines = new List<string>
            {
                $"Level {_data.CharacterLevel + 1}: {_class.ClassName} ({LevelUpRules.Ordinal(picks)} pick)",
                "Ability points: " + string.Join(", ", _alloc.Where(p => p.Value > 0).Select(p => $"+{p.Value} {p.Key}"))
            };

            if (_feature != null)
                lines.Add("Feature: " + _feature.Name + (_subFeature != null ? $" ({_subFeature.Name})" : string.Empty));

            var gains = new List<string>();
            if (_class.HitProtectionBonus > 0) gains.Add($"+{_class.HitProtectionBonus} Hit Protection");
            if (_class.FatigueBarBonus > 0) gains.Add($"+{_class.FatigueBarBonus} Fatigue Bar");
            if (_class.GrantsDicePool is { } pool) gains.Add($"+{_class.DicePerSelection} {DicePools.Name(pool)}");
            if (_class.FavourPerSelection > 0) gains.Add($"+{_class.FavourPerSelection} Favour");
            if (_class.TrainingPointsPerSelection > 0) gains.Add($"+{_class.TrainingPointsPerSelection} training points");
            if (_weapon != null) gains.Add($"{_weapon.Name} weapon skill proficiency");
            if (_quip != null) gains.Add($"quip {_quip.Name}");
            if (_class.RecurringChoice == ClassChoice.Construct && _constructName.Trim().Length > 0)
                gains.Add($"construct {_constructName.Trim()}");
            if (gains.Count > 0) lines.Add("Gains: " + string.Join(", ", gains));

            return string.Join("\n", lines);
        }
    }

    // What is still missing, or empty when the level can be taken.
    public string ProblemText
    {
        get
        {
            if (!AllocationComplete) return $"Place your remaining {PointsLeft} ability point(s).";
            var request = BuildRequest();
            if (request == null) return "Choose a class.";
            return _svc.Validate(request) ?? string.Empty;
        }
    }

    public bool HasProblem => ProblemText.Length > 0;
    public bool CanConfirm => ProblemText.Length == 0;

    private LevelUpRequest? BuildRequest()
    {
        if (_class == null) return null;

        ConstructSpec? construct = null;
        if (_class.RecurringChoice == ClassChoice.Construct)
        {
            int capacity = int.TryParse(_constructCapacity.Trim(), out int parsed) ? parsed : -1;
            construct = new ConstructSpec(_constructName, _constructDescription, capacity);
        }

        return new LevelUpRequest
        {
            AbilityPoints = new Dictionary<string, int>(_alloc),
            Class = _class,
            Feature = _feature,
            WarriorWeapon = _weapon,
            Quip = _quip,
            FeatureSubFeature = _subFeature,
            Construct = construct
        };
    }

    private async Task ConfirmAsync()
    {
        if (_busy) return;
        var request = BuildRequest();
        if (request == null || _svc.Validate(request) != null) return;

        var page = Application.Current!.MainPage!;
        var warnings = _svc.SoftLimitWarnings(_class!);
        string extra = string.Empty;
        if (warnings.Count > 0) extra += "\n\n" + string.Join("\n", warnings.Select(w => "⚠️ " + w));
        if (!SessionMode.GmHosted) extra += "\n\n" + RuleSkatingNote;

        bool ok = await page.DisplayAlertAsync($"Take level {_data.CharacterLevel + 1}?", SummaryText + extra, "Level Up", "Not yet");
        if (!ok) return;

        _busy = true;
        try
        {
            var result = await _svc.ApplyAsync(request);
            if (!result.Success)
            {
                await page.DisplayAlertAsync("Couldn't Level Up", result.Message, "OK");
                return;
            }

            if (result.Notes.Count > 0)
                await page.DisplayAlertAsync("Level taken, with notes", string.Join("\n", result.Notes), "OK");

            Finished?.Invoke();
        }
        finally { _busy = false; }
    }

    // ===== HELPERS =====
    private static PickOption Option(string title, string? subtitle, string? detail, object tag,
        Action<PickOption> picked, string search = "")
    {
        var option = new PickOption
        {
            Title = title,
            Subtitle = subtitle ?? string.Empty,
            Detail = detail ?? string.Empty,
            Tag = tag,
            SearchText = search
        };
        option.SelectCommand = new Command(() => picked(option));
        return option;
    }

    private static PickOption SubOption(SubFeatureEntry entry, Action<PickOption> picked)
        => Option(entry.Name, entry.DetailLine, entry.Description, entry, picked);

    private static void Mark(IEnumerable<PickOption> group, PickOption chosen)
    {
        foreach (var o in group) o.IsSelected = ReferenceEquals(o, chosen);
    }

    // The creators call back before they close themselves.
    private async Task CreateCustomAsync(SubFeatureType type, Action<SubFeatureEntry> added)
    {
        var services = IPlatformApplication.Current!.Services;

        void Created(object created)
        {
            var entry = SubFeatureEntry.From(created);
            MainThread.BeginInvokeOnMainThread(() => added(entry));
        }

        Page creator = type switch
        {
            SubFeatureType.Spell => new CharManJur.Views.CustomSpellCreatorPage(
                services.GetRequiredService<ISpellDataService>(), s => Created(s)),
            SubFeatureType.Quip => new CharManJur.Views.CustomQuipCreatorPage(
                services.GetRequiredService<IQuipDataService>(), q => Created(q)),
            SubFeatureType.Blueprint => new CharManJur.Views.CustomBlueprintCreatorPage(
                services.GetRequiredService<IBlueprintDataService>(), b => Created(b)),
            _ => new CharManJur.Views.CustomTechniqueCreatorPage(
                services.GetRequiredService<ITechniqueDataService>(), t => Created(t))
        };

        await Application.Current!.MainPage!.Navigation.PushModalAsync(creator);
    }

    private void RefreshSummary()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ProblemText));
        OnPropertyChanged(nameof(HasProblem));
        OnPropertyChanged(nameof(CanConfirm));
    }

    private void Refresh() => OnPropertyChanged(string.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}