using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels;

public class BackgroundSelectionViewModel : INotifyPropertyChanged
{
    private readonly IBackgroundDataService _backgroundDataService;
    private readonly ICharAttribDataService _charDataService;
    private readonly IItemDataService _itemDataService;
    private readonly IFamiliarDataService _familiarDataService;
    private readonly ILanguageDataService _languageDataService;

    private ObservableCollection<CharacterBackground> _backgrounds = new();
    private CharacterBackground? _selectedBackground;
    private bool _isLoading;

    private Dictionary<int, List<SelectableItem>> _itemChoiceSelections = new();
    private Dictionary<int, List<SelectableFamiliar>> _familiarChoiceSelections = new();

    public ObservableCollection<ItemChoiceDisplay> ItemChoiceDisplays { get; set; } = new();

    private ObservableCollection<LanguageDisplay> _languageDisplays = new();

    public ObservableCollection<LanguageDisplay> LanguageDisplays
    {
        get => _languageDisplays;
        set
        {
            _languageDisplays = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedLanguages => LanguageDisplays.Any(d => d.IsSelected);
    public string SelectedLanguagesDisplay => HasSelectedLanguages
        ? string.Join(", ", LanguageDisplays.Where(d => d.IsSelected).Select(d => d.Name))
        : "No languages selected";

    private ObservableCollection<FamiliarChoiceDisplay> _familiarChoiceDisplays = new();
    private SelectableFamiliar? _selectedFamiliar;

    public ObservableCollection<FamiliarChoiceDisplay> FamiliarChoiceDisplays
    {
        get => _familiarChoiceDisplays;
        set
        {
            _familiarChoiceDisplays = value;
            OnPropertyChanged();
        }
    }

    public SelectableFamiliar? SelectedFamiliar
    {
        get => _selectedFamiliar;
        set
        {
            if (_selectedFamiliar != value)
            {
                if (_selectedFamiliar != null)
                {
                    _selectedFamiliar.IsSelected = false;
                }

                _selectedFamiliar = value;

                if (_selectedFamiliar != null)
                {
                    _selectedFamiliar.IsSelected = true;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedFamiliar));
                OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
                OnPropertyChanged(nameof(CanConfirmBackground));
            }
        }
    }

    public bool HasSelectedFamiliar => SelectedFamiliar != null;

    public bool AreFamiliarChoicesSatisfied
    {
        get
        {
            if (!FamiliarChoiceDisplays.Any())
                return true;

            foreach (var display in FamiliarChoiceDisplays)
            {
                var selected = display.Options.Any(o => o.IsSelected);
                if (!selected) return false;
            }

            return true;
        }
    }

    public bool CanConfirmBackground
    {
        get
        {
            if (!HasSelectedBackground) return false;

            // Check if there are any familiar choices
            if (FamiliarChoiceDisplays.Any())
            {
                // For each familiar choice, check if at least one option is selected
                foreach (var display in FamiliarChoiceDisplays)
                {
                    if (!display.Options.Any(o => o.IsSelected))
                        return false;
                }
            }

            return true;
        }
    }

    public bool HasFamiliarChoices => FamiliarChoiceDisplays.Any();

    public ObservableCollection<CharacterBackground> Backgrounds
    {
        get => _backgrounds;
        set
        {
            _backgrounds = value;
            OnPropertyChanged();
        }
    }

    public CharacterBackground? SelectedBackground
    {
        get => _selectedBackground;
        set
        {
            if (_selectedBackground != value)
            {
                // Reset training points when background changes
                if (_selectedBackground != null && value != null && _selectedBackground.Id != value.Id)
                {
                    ResetTrainingPoints();
                }

                _selectedBackground = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedBackground));
                OnPropertyChanged(nameof(SelectedBackgroundName));
                OnPropertyChanged(nameof(SelectedBackgroundDescription));
                OnPropertyChanged(nameof(SelectedBackgroundVigorBonus));
                OnPropertyChanged(nameof(SelectedBackgroundAgilityBonus));
                OnPropertyChanged(nameof(SelectedBackgroundMindBonus));
                OnPropertyChanged(nameof(SelectedBackgroundSpiritBonus));
                OnPropertyChanged(nameof(SelectedBackgroundSkillBonuses));
                OnPropertyChanged(nameof(SelectedBackgroundStartingItems));
                OnPropertyChanged(nameof(SelectedBackgroundItemChoices));
                OnPropertyChanged(nameof(SelectedBackgroundFamiliarChoices));

                RestoreSavedLanguages();

                LoadItemChoices();
                LoadFamiliarChoices();
                Task.Run(ResolveItemDetails);
            }
        }
    }

    private void ResetTrainingPoints()
    {
        // Reset all skill training levels to -2 (base penalty, no training points)
        foreach (var skill in _charDataService.SkillTrainingLevels.Keys.ToList())
        {
            _charDataService.SkillTrainingLevels[skill] = -2;
        }

        // Reset AvailableTrainingPoints to 4 (base)
        _charDataService.AvailableTrainingPoints = 4;

        System.Diagnostics.Debug.WriteLine("=== Training points reset due to background change ===");
    }

    private void RestoreSavedLanguages()
    {
        if (_charDataService.SelectedLanguages == null || !_charDataService.SelectedLanguages.Any())
        {
            foreach (var display in LanguageDisplays)
            {
                display.IsSelected = false;
            }
            OnPropertyChanged(nameof(HasSelectedLanguages));
            OnPropertyChanged(nameof(SelectedLanguagesDisplay));
            return;
        }

        var savedIds = _charDataService.SelectedLanguages.Select(l => l.Id).ToList();

        foreach (var display in LanguageDisplays)
        {
            display.IsSelected = savedIds.Contains(display.Language.Id);
        }

        OnPropertyChanged(nameof(HasSelectedLanguages));
        OnPropertyChanged(nameof(SelectedLanguagesDisplay));

        System.Diagnostics.Debug.WriteLine($"=== Restored {_charDataService.SelectedLanguages.Count} languages ===");
    }

    private async Task ResolveItemDetails()
    {
        if (SelectedBackground?.StartingItems == null) return;

        foreach (var startingItem in SelectedBackground.StartingItems)
        {
            if (startingItem.ItemId > 0)
            {
                startingItem.ItemDetails = await _itemDataService.GetItemByIdAsync(startingItem.ItemId);
            }
        }

        OnPropertyChanged(nameof(SelectedBackgroundStartingItems));
    }

    public bool HasSelectedBackground => SelectedBackground != null;
    public string SelectedBackgroundName => SelectedBackground?.Name ?? "Select a Background";
    public string SelectedBackgroundDescription => SelectedBackground?.Description ?? "Description will appear here";
    public string SelectedBackgroundVigorBonus => SelectedBackground != null && SelectedBackground.VigorModifier != 0
        ? $"Vigor: {SelectedBackground.VigorModifier:+0;-0;0}"
        : "No Stat bonus";
    public string SelectedBackgroundAgilityBonus => SelectedBackground != null && SelectedBackground.AgilityModifier != 0
        ? $"Agility: {SelectedBackground.AgilityModifier:+0;-0;0}"
        : "No Stat bonus";
    public string SelectedBackgroundMindBonus => SelectedBackground != null && SelectedBackground.MindModifier != 0
        ? $"Mind: {SelectedBackground.MindModifier:+0;-0;0}"
        : "No Stat bonus";
    public string SelectedBackgroundSpiritBonus => SelectedBackground != null && SelectedBackground.SpiritModifier != 0
        ? $"Spirit: {SelectedBackground.SpiritModifier:+0;-0;0}"
        : "No Stat bonus";

    public string SelectedBackgroundSkillBonuses
    {
        get
        {
            if (SelectedBackground?.SkillBonuses == null || !SelectedBackground.SkillBonuses.Any())
                return "No skill bonuses";
            return string.Join("\n", SelectedBackground.SkillBonuses.Select(s => $"{s.SkillName}: +{s.Bonus}"));
        }
    }

    public ObservableCollection<StartingItem> SelectedBackgroundStartingItems => SelectedBackground?.StartingItems ?? new ObservableCollection<StartingItem>();
    public List<ItemChoice> SelectedBackgroundItemChoices => SelectedBackground?.ItemChoices ?? new List<ItemChoice>();
    public List<FamiliarChoice> SelectedBackgroundFamiliarChoices => SelectedBackground?.FamiliarChoices ?? new List<FamiliarChoice>();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<int, List<SelectableItem>> ItemChoiceSelections => _itemChoiceSelections;
    public Dictionary<int, List<SelectableFamiliar>> FamiliarChoiceSelections => _familiarChoiceSelections;

    public ICommand LoadBackgroundsCommand { get; }
    public ICommand SelectBackgroundCommand { get; }
    public ICommand ConfirmBackgroundCommand { get; }
    public ICommand CreateCustomItemCommand { get; }
    public ICommand CreateCustomFamiliarCommand { get; }
    public ICommand RemoveStartingItemCommand { get; }
    public ICommand ToggleLanguageCommand { get; }

    public BackgroundSelectionViewModel(
        IBackgroundDataService backgroundDataService,
        ICharAttribDataService charDataService,
        IItemDataService itemDataService,
        IFamiliarDataService familiarDataService,
        ILanguageDataService languageDataService)
    {
        _backgroundDataService = backgroundDataService;
        _charDataService = charDataService;
        _itemDataService = itemDataService;
        _familiarDataService = familiarDataService;
        _languageDataService = languageDataService;

        LoadBackgroundsCommand = new Command(async () => await LoadBackgroundsAsync());
        SelectBackgroundCommand = new Command<CharacterBackground>(OnBackgroundSelected);
        ConfirmBackgroundCommand = new Command(async () => await ConfirmBackgroundAsync(), () => CanConfirmBackground);
        CreateCustomItemCommand = new Command<ItemChoice>(OnCreateCustomItem);
        CreateCustomFamiliarCommand = new Command<FamiliarChoice>(OnCreateCustomFamiliar);
        RemoveStartingItemCommand = new Command<StartingItem>(OnRemoveStartingItem);
        ToggleLanguageCommand = new Command<LanguageDisplay>(OnToggleLanguage);

        Task.Run(LoadBackgroundsAsync);
        Task.Run(LoadLanguagesAsync);
    }

    private async Task LoadBackgroundsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var backgrounds = await _backgroundDataService.GetBackgroundsAsync();
            Backgrounds = new ObservableCollection<CharacterBackground>(backgrounds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading backgrounds: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadLanguagesAsync()
    {
        try
        {
            var languages = await _languageDataService.GetLanguagesAsync();

            var displays = new ObservableCollection<LanguageDisplay>();
            foreach (var language in languages)
            {
                displays.Add(new LanguageDisplay
                {
                    Language = language,
                    IsSelected = false
                });
            }

            LanguageDisplays = displays;

            RestoreSavedLanguages();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading languages: {ex.Message}");
        }
    }

    private void OnBackgroundSelected(CharacterBackground? background)
    {
        SelectedBackground = background;
    }

    private async Task LoadItemChoices()
    {
        ItemChoiceDisplays.Clear();

        if (SelectedBackground?.ItemChoices == null) return;

        foreach (var choice in SelectedBackground.ItemChoices)
        {
            var display = new ItemChoiceDisplay { Choice = choice };

            if (choice.QueryCriteria == null)
            {
                choice.QueryCriteria = new ItemQueryCriteria();
            }

            var items = await _itemDataService.QueryItemsAsync(choice.QueryCriteria);

            foreach (var item in items)
            {
                display.Options.Add(new SelectableItem
                {
                    ItemId = item.Id,
                    DisplayName = item.Name,
                    Quantity = 1,
                    IsSelected = false,
                    IsCustom = item.IsPlayerCreated
                });
            }
            ItemChoiceDisplays.Add(display);
        }

        OnPropertyChanged(nameof(ItemChoiceDisplays));
        OnPropertyChanged(nameof(SelectedBackgroundItemChoices));
    }

    private async Task LoadFamiliarChoices()
    {
        FamiliarChoiceDisplays.Clear();
        SelectedFamiliar = null;

        if (SelectedBackground?.FamiliarChoices == null) return;

        foreach (var choice in SelectedBackground.FamiliarChoices)
        {
            var display = new FamiliarChoiceDisplay { Choice = choice };

            System.Diagnostics.Debug.WriteLine($"=== Loading familiars for choice: {choice.Id} ===");
            System.Diagnostics.Debug.WriteLine($"  Size: {choice.QueryCriteria?.Size ?? "None"}");
            System.Diagnostics.Debug.WriteLine($"  Species: {choice.QueryCriteria?.Species ?? "None"}");
            System.Diagnostics.Debug.WriteLine($"  Intelligence: {choice.QueryCriteria?.Intelligence ?? "None"}");

            var familiars = await _familiarDataService.QueryFamiliarsAsync(choice.QueryCriteria);

            System.Diagnostics.Debug.WriteLine($"  Found {familiars.Count} familiars");

            foreach (var familiar in familiars)
            {
                var selectable = new SelectableFamiliar
                {
                    FamiliarId = familiar.Id,
                    DisplayName = familiar.FmlrName ?? "Unnamed Familiar",
                    IsSelected = false,
                    IsCustom = false,
                    Familiar = familiar,
                    SelectCommand = new Command<SelectableFamiliar>(OnFamiliarSelected)
                };

                display.Options.Add(selectable);
            }

            FamiliarChoiceDisplays.Add(display);
        }

        OnPropertyChanged(nameof(FamiliarChoiceDisplays));
        OnPropertyChanged(nameof(HasFamiliarChoices));
        OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
        OnPropertyChanged(nameof(CanConfirmBackground));
    }

    private void OnFamiliarSelected(SelectableFamiliar? selected)
    {
        if (selected == null) return;

        System.Diagnostics.Debug.WriteLine($"=== OnFamiliarSelected called: {selected.DisplayName} ===");

        var display = FamiliarChoiceDisplays.FirstOrDefault(d => d.Options.Contains(selected));
        if (display != null)
        {
            foreach (var option in display.Options)
            {
                if (option != selected && option.IsSelected)
                {
                    option.IsSelected = false;
                }
            }
        }

        selected.IsSelected = !selected.IsSelected;

        if (!selected.IsSelected)
        {
            SelectedFamiliar = null;
            System.Diagnostics.Debug.WriteLine($"=== Familiar deselected ===");
        }
        else
        {
            SelectedFamiliar = selected;
            System.Diagnostics.Debug.WriteLine($"=== Familiar selected: {selected.DisplayName} ===");
        }

        OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
        OnPropertyChanged(nameof(CanConfirmBackground));
        OnPropertyChanged(nameof(FamiliarChoiceDisplays));
        OnPropertyChanged(nameof(SelectedFamiliar));
    }

    private void OnToggleLanguage(LanguageDisplay? display)
    {
        if (display == null) return;

        display.IsSelected = !display.IsSelected;

        OnPropertyChanged(nameof(HasSelectedLanguages));
        OnPropertyChanged(nameof(SelectedLanguagesDisplay));
    }

    private async void OnCreateCustomItem(ItemChoice choice)
    {
        if (choice == null) return;

        var display = ItemChoiceDisplays.FirstOrDefault(d => d.Choice.Id == choice.Id);
        if (display == null) return;

        System.Diagnostics.Debug.WriteLine($"=== Create Custom Item for choice: {choice.Id} ===");

        Action<Item> onItemCreated = (newItem) =>
        {
            System.Diagnostics.Debug.WriteLine($"=== Item created: {newItem.Name} (ID: {newItem.Id}) ===");

            display.Options.Add(new SelectableItem
            {
                ItemId = newItem.Id,
                DisplayName = newItem.Name,
                Quantity = 1,
                IsSelected = true,
                IsCustom = true,
                CustomTemplate = newItem
            });

            if (SelectedBackground != null)
            {
                if (SelectedBackground.StartingItems == null)
                {
                    SelectedBackground.StartingItems = new ObservableCollection<StartingItem>();
                }

                var newStartingItem = new StartingItem
                {
                    ItemId = newItem.Id,
                    Quantity = 1,
                    PlayerNote = "Custom item created during background selection"
                };
                newStartingItem.ItemDetails = newItem;

                SelectedBackground.StartingItems.Add(newStartingItem);

                OnPropertyChanged(nameof(SelectedBackgroundStartingItems));
            }

            OnPropertyChanged(nameof(ItemChoiceDisplays));
        };

        var creatorViewModel = new CustomItemCreatorViewModel(
            _itemDataService,
            onItemCreated,
            isLiveGame: false
        );

        var creatorPage = new CustomItemCreatorPage(creatorViewModel);
        await Shell.Current.Navigation.PushModalAsync(creatorPage);
    }

    private async void OnCreateCustomFamiliar(FamiliarChoice choice)
    {
        if (choice == null) return;

        System.Diagnostics.Debug.WriteLine($"=== Create Custom Familiar for choice: {choice.Id} ===");

        Action<Familiar> onFamiliarCreated = (newFamiliar) =>
        {
            System.Diagnostics.Debug.WriteLine($"=== Familiar created: {newFamiliar.FmlrName} (ID: {newFamiliar.Id}) ===");

            var display = FamiliarChoiceDisplays.FirstOrDefault(d => d.Choice.Id == choice.Id);
            if (display != null)
            {
                display.Options.Add(new SelectableFamiliar
                {
                    FamiliarId = newFamiliar.Id,
                    DisplayName = newFamiliar.FmlrName ?? "Unnamed Familiar",
                    IsSelected = true,
                    IsCustom = true,
                    Familiar = newFamiliar
                });
            }

            _charDataService.AddFamiliar(newFamiliar);

            OnPropertyChanged(nameof(FamiliarChoiceDisplays));
            OnPropertyChanged(nameof(HasFamiliarChoices));
            OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
            OnPropertyChanged(nameof(CanConfirmBackground));

            System.Diagnostics.Debug.WriteLine($"=== Familiar added to list. Total options: {display?.Options.Count ?? 0} ===");
        };

        var creatorPage = new CustomFamiliarCreatorPage(_familiarDataService, onFamiliarCreated);
        await Shell.Current.Navigation.PushModalAsync(creatorPage);
    }

    private async void OnRemoveStartingItem(StartingItem? item)
    {
        if (item == null || SelectedBackground?.StartingItems == null) return;

        bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Remove Item",
            $"Remove '{item.ItemDetails?.Name ?? "Item"}' from starting items?",
            "Yes, Remove",
            "Cancel");

        if (!confirm) return;

        SelectedBackground.StartingItems.Remove(item);

        foreach (var display in ItemChoiceDisplays)
        {
            var option = display.Options.FirstOrDefault(o => o.ItemId == item.ItemId);
            if (option != null)
            {
                display.Options.Remove(option);
                break;
            }
        }

        OnPropertyChanged(nameof(SelectedBackgroundStartingItems));
        OnPropertyChanged(nameof(ItemChoiceDisplays));

        System.Diagnostics.Debug.WriteLine($"=== Removed starting item: {item.ItemDetails?.Name ?? "Item"} ===");
    }

    private async Task ConfirmBackgroundAsync()
    {
        if (SelectedBackground == null) return;

        _charDataService.SelectedBackgroundName = SelectedBackground.Name;
        _charDataService.SelectedBackgroundDescription = SelectedBackground.Description;

        _charDataService.BGVigorBonus = SelectedBackground.VigorModifier;
        _charDataService.BGAgilityBonus = SelectedBackground.AgilityModifier;
        _charDataService.BGMindBonus = SelectedBackground.MindModifier;
        _charDataService.BGSpiritBonus = SelectedBackground.SpiritModifier;

        _charDataService.SelectedStartingItems = SelectedBackground.StartingItems?.ToList() ?? new List<StartingItem>();

        _charDataService.SelectedSkillBonuses = SelectedBackground.SkillBonuses?.ToList() ?? new List<BGSkillBonuses>();

        _charDataService.SelectedItemChoices = SelectedBackground.ItemChoices?.ToList() ?? new List<ItemChoice>();

        var selectedLanguages = LanguageDisplays
            .Where(d => d.IsSelected)
            .Select(d => d.Language)
            .ToList();
        _charDataService.SelectedLanguages = selectedLanguages;

        // === SAVE SELECTED FAMILIAR ===
        if (SelectedFamiliar != null && SelectedFamiliar.Familiar != null)
        {
            _charDataService.AddFamiliar(SelectedFamiliar.Familiar);
            System.Diagnostics.Debug.WriteLine($"=== Saved familiar: {SelectedFamiliar.Familiar.FmlrName} ===");
        }

        System.Diagnostics.Debug.WriteLine($"=== Confirmed Background: {SelectedBackground.Name} ===");

        await Shell.Current.GoToAsync("///CharBuilder_Godrick_HinderanceSelection");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SelectableItem
{
    public int ItemId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public bool IsSelected { get; set; }
    public bool IsCustom { get; set; }
    public Item? CustomTemplate { get; set; }
}

public class SelectableFamiliar
{
    public int FamiliarId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsCustom { get; set; }
    public Familiar? Familiar { get; set; }
    public Familiar? CustomFamiliar { get; set; }
    public ICommand? SelectCommand { get; set; }
}

public class FamiliarChoiceDisplay
{
    public FamiliarChoice Choice { get; set; } = new();
    public List<SelectableFamiliar> Options { get; set; } = new();
}