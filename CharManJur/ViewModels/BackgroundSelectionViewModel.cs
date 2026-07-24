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

    public ObservableCollection<FamiliarChoiceDisplay> FamiliarChoiceDisplays
    {
        get => _familiarChoiceDisplays;
        set
        {
            _familiarChoiceDisplays = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFamiliarChoices));
            OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
            OnPropertyChanged(nameof(CanConfirmBackground));
            // Refresh command state
            ((Command)ConfirmBackgroundCommand)?.ChangeCanExecute();
        }
    }

    public bool HasFamiliarChoices => FamiliarChoiceDisplays.Any();

    // Each choice group must have a selection
    public bool AreFamiliarChoicesSatisfied
    {
        get
        {
            if (!FamiliarChoiceDisplays.Any())
                return true;

            foreach (var display in FamiliarChoiceDisplays)
            {
                if (!display.Options.Any(o => o.IsSelected))
                    return false;
            }

            return true;
        }
    }

    // Each choice group must have a selection for CanConfirmBackground
    public bool CanConfirmBackground
    {
        get
        {
            if (!HasSelectedBackground) return false;

            if (FamiliarChoiceDisplays.Any())
            {
                foreach (var display in FamiliarChoiceDisplays)
                {
                    if (!display.Options.Any(o => o.IsSelected))
                        return false;
                }
            }

            return true;
        }
    }

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
                if (_selectedBackground != null && value != null && _selectedBackground.Id != value.Id)
                {
                    ResetTrainingPoints();
                }

                _selectedBackground = value?.Clone();
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
        foreach (var skill in _charDataService.SkillTrainingLevels.Keys.ToList())
        {
            _charDataService.SkillTrainingLevels[skill] = -2;
        }

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
        : "";
    public string SelectedBackgroundAgilityBonus => SelectedBackground != null && SelectedBackground.AgilityModifier != 0
        ? $"Agility: {SelectedBackground.AgilityModifier:+0;-0;0}"
        : "";
    public string SelectedBackgroundMindBonus => SelectedBackground != null && SelectedBackground.MindModifier != 0
        ? $"Mind: {SelectedBackground.MindModifier:+0;-0;0}"
        : "";
    public string SelectedBackgroundSpiritBonus => SelectedBackground != null && SelectedBackground.SpiritModifier != 0
        ? $"Spirit: {SelectedBackground.SpiritModifier:+0;-0;0}"
        : "";

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
    public ICommand CreateCustomBackgroundCommand { get; }
    public ICommand EditCustomBackgroundCommand { get; }
    public ICommand DeleteCustomBackgroundCommand { get; }

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
        ConfirmBackgroundCommand = new Command(async () => await ConfirmBackgroundAsync());
        CreateCustomItemCommand = new Command<ItemChoice>(OnCreateCustomItem);
        CreateCustomFamiliarCommand = new Command<FamiliarChoice>(OnCreateCustomFamiliar);
        RemoveStartingItemCommand = new Command<StartingItem>(OnRemoveStartingItem);
        ToggleLanguageCommand = new Command<LanguageDisplay>(OnToggleLanguage);
        CreateCustomBackgroundCommand = new Command(async () => await CreateCustomBackgroundAsync());
        EditCustomBackgroundCommand = new Command<CharacterBackground>(async (bg) => await EditCustomBackgroundAsync(bg));
        DeleteCustomBackgroundCommand = new Command<CharacterBackground>(async (bg) => await DeleteCustomBackgroundAsync(bg));

        Task.Run(LoadBackgroundsAsync);
        Task.Run(LoadLanguagesAsync);
    }

    private async Task LoadBackgroundsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var backgrounds = await _backgroundDataService.GetAllBackgroundsAsync();
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

            // Make sure the QueryCriteria is properly set up for filtering
            var criteria = choice.QueryCriteria;

            // Ensure categories are properly set for filtering
            if (criteria.AllowedCategories != null && criteria.AllowedCategories.Any())
            {
                // Categories are already set
                System.Diagnostics.Debug.WriteLine($"Item choice {choice.Id} has {criteria.AllowedCategories.Count} categories");
                foreach (var cat in criteria.AllowedCategories)
                {
                    System.Diagnostics.Debug.WriteLine($"  Category: {cat}");
                }
            }
            else if (criteria.Category.HasValue)
            {
                // Single category - add to AllowedCategories for consistency
                criteria.AllowedCategories = new List<ItemCategory> { criteria.Category.Value };
                System.Diagnostics.Debug.WriteLine($"Item choice {choice.Id} has single category: {criteria.Category.Value}");
            }

            var items = await _itemDataService.QueryItemsAsync(criteria);

            System.Diagnostics.Debug.WriteLine($"Found {items.Count} items for choice {choice.Id}");

            foreach (var item in items)
            {
                display.Options.Add(new SelectableItem
                {
                    ItemId = item.Id,
                    DisplayName = item.Name,
                    Quantity = 1,
                    IsSelected = false,
                    IsCustom = item.IsPlayerCreated,
                    ItemDetails = item
                });
            }
            ItemChoiceDisplays.Add(display);
        }

        OnPropertyChanged(nameof(ItemChoiceDisplays));
        OnPropertyChanged(nameof(SelectedBackgroundItemChoices));
    }

    private async Task CreateCustomBackgroundAsync()
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "RefreshOnReturn", true }
        };
        await Shell.Current.GoToAsync("///Godrick_CustomBackgroundCreator", navigationParameters);
    }

    public async Task EditCustomBackgroundAsync(CharacterBackground? background)
    {
        if (background == null) return;

        System.Diagnostics.Debug.WriteLine($"=== EditCustomBackgroundAsync called for: {background.Name} (ID: {background.Id}) ===");

        if (background.Id < 90001)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Cannot Edit",
                "Foundation backgrounds cannot be edited.",
                "OK");
            return;
        }

        var navigationParameters = new Dictionary<string, object>
        {
            { "BackgroundToEdit", background }
        };
        await Shell.Current.GoToAsync("///Godrick_CustomBackgroundCreator", navigationParameters);
    }

    public async Task DeleteCustomBackgroundAsync(CharacterBackground? background)
    {
        if (background == null) return;

        if (background.Id < 90001)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Cannot Delete",
                "Foundation backgrounds cannot be deleted.",
                "OK");
            return;
        }

        var confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Delete Custom Background",
            $"Are you sure you want to delete '{background.Name}'? This action cannot be undone.",
            "Yes, Delete",
            "No, Cancel");

        if (!confirm) return;

        try
        {
            var customBgStorage = Application.Current.Handler?.MauiContext?.Services?.GetService<ICustomBackgroundStorageService>();
            if (customBgStorage != null)
            {
                var deleted = await customBgStorage.DeleteCustomBackgroundAsync(background.Id);
                if (deleted)
                {
                    Backgrounds.Remove(background);
                    if (SelectedBackground?.Id == background.Id)
                    {
                        SelectedBackground = null;
                    }
                    await Application.Current.MainPage.DisplayAlertAsync(
                        "Success",
                        $"Background '{background.Name}' has been deleted.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Error",
                $"Failed to delete background: {ex.Message}",
                "OK");
        }
    }

    public async Task RefreshBackgroundsAsync()
    {
        try
        {
            var backgrounds = await _backgroundDataService.GetAllBackgroundsAsync();
            Backgrounds = new ObservableCollection<CharacterBackground>(backgrounds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing backgrounds: {ex.Message}");
        }
    }

    private async Task LoadFamiliarChoices()
    {
        FamiliarChoiceDisplays.Clear();

        if (SelectedBackground?.FamiliarChoices == null) return;

        foreach (var choice in SelectedBackground.FamiliarChoices)
        {
            var display = new FamiliarChoiceDisplay { Choice = choice };

            System.Diagnostics.Debug.WriteLine($"=== Loading familiars for choice: {choice.Id} ===");

            if (choice.QueryCriteria != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Sizes: {string.Join(", ", choice.QueryCriteria.AllowedSizes ?? new List<string>())}");
                System.Diagnostics.Debug.WriteLine($"  Intelligences: {string.Join(", ", choice.QueryCriteria.AllowedIntelligences ?? new List<string>())}");
                System.Diagnostics.Debug.WriteLine($"  Species: {string.Join(", ", choice.QueryCriteria.AllowedSpecies ?? new List<string>())}");
            }

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

        // Find which choice group this familiar belongs to
        var parentDisplay = FamiliarChoiceDisplays.FirstOrDefault(d => d.Options.Contains(selected));
        if (parentDisplay == null) return;

        // If this familiar is already selected, deselect it (toggle off)
        if (selected.IsSelected)
        {
            selected.IsSelected = false;

            OnPropertyChanged(nameof(FamiliarChoiceDisplays));
            OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
            OnPropertyChanged(nameof(CanConfirmBackground));
            ((Command)ConfirmBackgroundCommand)?.ChangeCanExecute();
            return;
        }

        // Deselect ALL familiars in the SAME choice group only
        foreach (var option in parentDisplay.Options)
        {
            if (option != selected && option.IsSelected)
            {
                option.IsSelected = false;
                System.Diagnostics.Debug.WriteLine($"=== Deselecting: {option.DisplayName} ===");
            }
        }

        // Select this familiar
        selected.IsSelected = true;

        OnPropertyChanged(nameof(FamiliarChoiceDisplays));
        OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
        OnPropertyChanged(nameof(CanConfirmBackground));
        ((Command)ConfirmBackgroundCommand)?.ChangeCanExecute();

        System.Diagnostics.Debug.WriteLine($"=== Selected familiar: {selected.DisplayName} ===");
        System.Diagnostics.Debug.WriteLine($"=== AreFamiliarChoicesSatisfied: {AreFamiliarChoicesSatisfied} ===");
        System.Diagnostics.Debug.WriteLine($"=== CanConfirmBackground: {CanConfirmBackground} ===");
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
                CustomTemplate = newItem,
                ItemDetails = newItem
            });

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

        Action<Familiar> onFamiliarCreated = async (newFamiliar) =>
        {
            System.Diagnostics.Debug.WriteLine($"=== Familiar created: {newFamiliar.FmlrName} (ID: {newFamiliar.Id}) ===");

            var display = FamiliarChoiceDisplays.FirstOrDefault(d => d.Choice.Id == choice.Id);
            if (display != null)
            {
                // Reload all familiars to include the new one
                var familiars = await _familiarDataService.QueryFamiliarsAsync(display.Choice.QueryCriteria);

                display.Options.Clear();
                foreach (var familiar in familiars)
                {
                    var selectable = new SelectableFamiliar
                    {
                        FamiliarId = familiar.Id,
                        DisplayName = familiar.FmlrName ?? "Unnamed Familiar",
                        IsSelected = false,
                        IsCustom = familiar.IsPlayerCreated,
                        Familiar = familiar,
                        SelectCommand = new Command<SelectableFamiliar>(OnFamiliarSelected)
                    };
                    display.Options.Add(selectable);
                }

                // Auto-select the newly created familiar
                var newOption = display.Options.FirstOrDefault(o => o.FamiliarId == newFamiliar.Id);
                if (newOption != null)
                {
                    newOption.IsSelected = true;
                }
            }

            _charDataService.AddFamiliar(newFamiliar);

            OnPropertyChanged(nameof(FamiliarChoiceDisplays));
            OnPropertyChanged(nameof(HasFamiliarChoices));
            OnPropertyChanged(nameof(AreFamiliarChoicesSatisfied));
            OnPropertyChanged(nameof(CanConfirmBackground));
            ((Command)ConfirmBackgroundCommand)?.ChangeCanExecute();

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

        // ===== VALIDATE FAMILIAR SELECTIONS =====
        if (FamiliarChoiceDisplays.Any())
        {
            var missingGroups = new List<string>();

            foreach (var display in FamiliarChoiceDisplays)
            {
                if (!display.Options.Any(o => o.IsSelected))
                {
                    missingGroups.Add(display.Choice.Prompt ?? $"Choice {display.Choice.Id}");
                }
            }

            if (missingGroups.Any())
            {
                string message;
                if (missingGroups.Count == 1)
                {
                    message = $"Please select a familiar for:\n\n• {missingGroups[0]}";
                }
                else
                {
                    message = $"Please select a familiar for the following choice(s):\n\n";
                    message += string.Join("\n", missingGroups.Select(g => $"• {g}"));
                }

                await Application.Current.MainPage.DisplayAlertAsync(
                    "Selection Required",
                    message + "\n\nYou must make a selection for each before continuing.",
                    "OK");
                return;
            }
        }

        _charDataService.SelectedBackgroundName = SelectedBackground.Name;
        _charDataService.SelectedBackgroundDescription = SelectedBackground.Description;

        _charDataService.BGVigorBonus = SelectedBackground.VigorModifier;
        _charDataService.BGAgilityBonus = SelectedBackground.AgilityModifier;
        _charDataService.BGMindBonus = SelectedBackground.MindModifier;
        _charDataService.BGSpiritBonus = SelectedBackground.SpiritModifier;

        // === MERGE ITEM CHOICE SELECTIONS INTO STARTING ITEMS ===
        foreach (var display in ItemChoiceDisplays)
        {
            var selectedOptions = display.Options.Where(o => o.IsSelected);
            foreach (var option in selectedOptions)
            {
                SelectedBackground.StartingItems ??= new ObservableCollection<StartingItem>();

                SelectedBackground.StartingItems.Add(new StartingItem
                {
                    ItemId = option.ItemId,
                    Quantity = option.Quantity,
                    ItemDetails = option.ItemDetails
                });
            }
        }

        _charDataService.SelectedStartingItems = SelectedBackground.StartingItems?.ToList() ?? new List<StartingItem>();

        _charDataService.SelectedSkillBonuses = SelectedBackground.SkillBonuses?.ToList() ?? new List<BGSkillBonuses>();

        _charDataService.SelectedItemChoices = SelectedBackground.ItemChoices?.ToList() ?? new List<ItemChoice>();

        var selectedLanguages = LanguageDisplays
            .Where(d => d.IsSelected)
            .Select(d => d.Language)
            .ToList();
        _charDataService.SelectedLanguages = selectedLanguages;

        // === SAVE SELECTED FAMILIARS FROM EACH CHOICE GROUP ===
        foreach (var display in FamiliarChoiceDisplays)
        {
            var selectedFamiliar = display.Options.FirstOrDefault(o => o.IsSelected);
            if (selectedFamiliar != null && selectedFamiliar.Familiar != null)
            {
                _charDataService.AddFamiliar(selectedFamiliar.Familiar);
                System.Diagnostics.Debug.WriteLine($"=== Saved familiar: {selectedFamiliar.Familiar.FmlrName} from choice: {display.Choice.Prompt} ===");
            }
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
    public Item? ItemDetails { get; set; }
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