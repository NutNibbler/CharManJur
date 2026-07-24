using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels;

public class CustomBackgroundBuilderViewModel : INotifyPropertyChanged
{
    private readonly ICustomBackgroundStorageService _customBackgroundStorage;
    private readonly IBackgroundDataService _backgroundDataService;
    private readonly IItemDataService _itemDataService;
    private readonly IFamiliarDataService _familiarDataService;
    private readonly ICharAttribDataService _charDataService;

    // ===== BACKGROUND PROPERTIES =====
    private string _name = string.Empty;
    private string _description = string.Empty;
    private int _vigorModifier = 0;
    private int _agilityModifier = 0;
    private int _mindModifier = 0;
    private int _spiritModifier = 0;

    // ===== SKILL BONUSES =====
    private ObservableCollection<BGSkillBonuses> _skillBonuses = new();
    private string _selectedSkillName = string.Empty;
    private int _selectedSkillBonus = 1;

    // ===== STARTING ITEMS =====
    private ObservableCollection<StartingItem> _startingItems = new();
    private Item? _selectedStartingItem;
    private int _selectedStartingItemQuantity = 1;

    // ===== FAMILIAR CHOICES =====
    private ObservableCollection<FamiliarChoiceConfig> _familiarChoices = new();
    private bool _hasFamiliarChoices = false;

    // ===== ITEM CHOICES =====
    private ObservableCollection<ItemChoiceConfig> _itemChoices = new();
    private bool _hasItemChoices = false;

    // ===== CLONE FROM EXISTING =====
    private List<CharacterBackground> _allBackgrounds = new();
    private CharacterBackground? _selectedCloneSource;
    private bool _isEditMode;
    private CharacterBackground? _editingBackground;

    // ===== PREDEFINED SKILLS =====
    public List<string> PredefinedSkills { get; } = new()
    {
        "Athletics", "Acrobatics", "Aim", "Arcana", "Artifice",
        "Commune", "Constitution", "Deception", "Diplomacy", "Drive",
        "Grapple", "Heal", "Investigate", "Lore", "Sight",
        "Presence", "Ride", "Stealth", "Survival", "Thief"
    };

    // ===== AVAILABLE FILTER OPTIONS =====
    public List<string> AvailableSizes { get; } = new() { "Tiny", "Small", "Medium", "Large", "Giant" };
    public List<string> AvailableIntelligences { get; } = new() { "Wild", "SemiDomestic", "Domestic", "Sapient" };
    public List<string> AvailableSpecies { get; } = new() { "Aves", "Reptilia", "Amphibia", "Mammalia", "Anthropoda", "Mollusca", "Annelida" };
    public List<ItemCategory> AvailableItemCategories { get; } = new()
    {
        ItemCategory.Weapon,
        ItemCategory.Armor,
        ItemCategory.Food,
        ItemCategory.Essential,
        ItemCategory.ToolKit,
        ItemCategory.AdventuringGear,
        ItemCategory.Resource,
        ItemCategory.Currency,
        ItemCategory.Instrument,
        ItemCategory.Miscellaneous,
        ItemCategory.Jewelry,
        ItemCategory.Unknown,
        ItemCategory.Story
    };

    // ===== PROPERTIES =====
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public int VigorModifier
    {
        get => _vigorModifier;
        set { _vigorModifier = value; OnPropertyChanged(); }
    }

    public int AgilityModifier
    {
        get => _agilityModifier;
        set { _agilityModifier = value; OnPropertyChanged(); }
    }

    public int MindModifier
    {
        get => _mindModifier;
        set { _mindModifier = value; OnPropertyChanged(); }
    }

    public int SpiritModifier
    {
        get => _spiritModifier;
        set { _spiritModifier = value; OnPropertyChanged(); }
    }

    public ObservableCollection<BGSkillBonuses> SkillBonuses
    {
        get => _skillBonuses;
        set { _skillBonuses = value; OnPropertyChanged(); }
    }

    public string SelectedSkillName
    {
        get => _selectedSkillName;
        set { _selectedSkillName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkill)); }
    }

    public int SelectedSkillBonus
    {
        get => _selectedSkillBonus;
        set { _selectedSkillBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkill)); }
    }

    public ObservableCollection<StartingItem> StartingItems
    {
        get => _startingItems;
        set { _startingItems = value; OnPropertyChanged(); }
    }

    public Item? SelectedStartingItem
    {
        get => _selectedStartingItem;
        set { _selectedStartingItem = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddStartingItem)); }
    }

    public int SelectedStartingItemQuantity
    {
        get => _selectedStartingItemQuantity;
        set { _selectedStartingItemQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddStartingItem)); }
    }

    public ObservableCollection<FamiliarChoiceConfig> FamiliarChoices
    {
        get => _familiarChoices;
        set { _familiarChoices = value; OnPropertyChanged(); }
    }

    public bool HasFamiliarChoices
    {
        get => _hasFamiliarChoices;
        set { _hasFamiliarChoices = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ItemChoiceConfig> ItemChoices
    {
        get => _itemChoices;
        set { _itemChoices = value; OnPropertyChanged(); }
    }

    public bool HasItemChoices
    {
        get => _hasItemChoices;
        set { _hasItemChoices = value; OnPropertyChanged(); }
    }

    public List<CharacterBackground> AllBackgrounds
    {
        get => _allBackgrounds;
        set { _allBackgrounds = value; OnPropertyChanged(); }
    }

    public CharacterBackground? SelectedCloneSource
    {
        get => _selectedCloneSource;
        set
        {
            _selectedCloneSource = value;
            OnPropertyChanged();
            if (value != null && !IsEditMode)
            {
                CloneFromBackground(value);
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(SaveButtonText));
                OnPropertyChanged(nameof(ShowCloneSection));
                System.Diagnostics.Debug.WriteLine($"IsEditMode changed to: {value}");
            }
        }
    }

    public bool ShowCloneSection => !IsEditMode;

    public string PageTitle => IsEditMode ? "Edit Custom Background" : "Create Custom Background";
    public string SaveButtonText => IsEditMode ? "Update Background" : "Create Background";

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);
    public bool CanAddSkill => !string.IsNullOrWhiteSpace(SelectedSkillName) && SelectedSkillBonus != 0;
    public bool CanAddStartingItem => SelectedStartingItem != null && SelectedStartingItemQuantity > 0;

    // ===== AVAILABLE ITEMS FOR SELECTION =====
    private List<Item> _availableItems = new();
    public List<Item> AvailableItems
    {
        get => _availableItems;
        set { _availableItems = value; OnPropertyChanged(); }
    }

    // ===== COMMANDS =====
    public ICommand AddSkillCommand { get; }
    public ICommand RemoveSkillCommand { get; }
    public ICommand AddStartingItemCommand { get; }
    public ICommand RemoveStartingItemCommand { get; }
    public ICommand CreateCustomItemCommand { get; }

    // Familiar Commands
    public ICommand AddFamiliarChoiceCommand { get; }
    public ICommand RemoveFamiliarChoiceCommand { get; }
    public ICommand RemoveFamiliarFilterCommand { get; }
    public ICommand CreateCustomFamiliarCommand { get; }
    public ICommand AddSizeFilterCommand { get; }
    public ICommand AddIntelligenceFilterCommand { get; }
    public ICommand AddSpeciesFilterCommand { get; }

    // Item Choice Commands
    public ICommand AddItemChoiceCommand { get; }
    public ICommand RemoveItemChoiceCommand { get; }
    public ICommand AddItemChoiceCategoryCommand { get; }
    public ICommand RemoveItemChoiceCategoryCommand { get; }

    public ICommand SaveBackgroundCommand { get; }
    public ICommand CancelCommand { get; }

    // Stepper Commands
    public ICommand IncrementVigorCommand { get; }
    public ICommand DecrementVigorCommand { get; }
    public ICommand IncrementAgilityCommand { get; }
    public ICommand DecrementAgilityCommand { get; }
    public ICommand IncrementMindCommand { get; }
    public ICommand DecrementMindCommand { get; }
    public ICommand IncrementSpiritCommand { get; }
    public ICommand DecrementSpiritCommand { get; }
    public ICommand IncrementSkillBonusCommand { get; }
    public ICommand DecrementSkillBonusCommand { get; }
    public ICommand IncrementQuantityCommand { get; }
    public ICommand DecrementQuantityCommand { get; }

    public CustomBackgroundBuilderViewModel(
        ICustomBackgroundStorageService customBackgroundStorage,
        IBackgroundDataService backgroundDataService,
        IItemDataService itemDataService,
        IFamiliarDataService familiarDataService,
        ICharAttribDataService charDataService)
    {
        _customBackgroundStorage = customBackgroundStorage;
        _backgroundDataService = backgroundDataService;
        _itemDataService = itemDataService;
        _familiarDataService = familiarDataService;
        _charDataService = charDataService;

        AddSkillCommand = new Command(AddSkill);
        RemoveSkillCommand = new Command<BGSkillBonuses>(RemoveSkill);
        AddStartingItemCommand = new Command(AddStartingItem);
        RemoveStartingItemCommand = new Command<StartingItem>(RemoveStartingItem);
        CreateCustomItemCommand = new Command(async () => await CreateCustomItemAsync());

        // Familiar Commands
        AddFamiliarChoiceCommand = new Command(AddFamiliarChoice);
        RemoveFamiliarChoiceCommand = new Command<FamiliarChoiceConfig>(RemoveFamiliarChoice);
        RemoveFamiliarFilterCommand = new Command<FilterValue>(RemoveFamiliarFilter);
        CreateCustomFamiliarCommand = new Command<FamiliarChoiceConfig>(async (config) => await CreateCustomFamiliarAsync(config));
        AddSizeFilterCommand = new Command<FamiliarChoiceConfig>(AddSizeFilter);
        AddIntelligenceFilterCommand = new Command<FamiliarChoiceConfig>(AddIntelligenceFilter);
        AddSpeciesFilterCommand = new Command<FamiliarChoiceConfig>(AddSpeciesFilter);

        // Item Choice Commands
        AddItemChoiceCommand = new Command(AddItemChoice);
        RemoveItemChoiceCommand = new Command<ItemChoiceConfig>(RemoveItemChoice);
        AddItemChoiceCategoryCommand = new Command<ItemChoiceConfig>(AddItemChoiceCategory);
        RemoveItemChoiceCategoryCommand = new Command<CategoryFilterValue>(RemoveItemChoiceCategory);

        SaveBackgroundCommand = new Command(async () => await SaveBackgroundAsync());
        CancelCommand = new Command(async () => await CancelAsync());

        // Stepper Commands
        IncrementVigorCommand = new Command(() => ChangeStat(ref _vigorModifier, 1, -10, 10, nameof(VigorModifier)));
        DecrementVigorCommand = new Command(() => ChangeStat(ref _vigorModifier, -1, -10, 10, nameof(VigorModifier)));
        IncrementAgilityCommand = new Command(() => ChangeStat(ref _agilityModifier, 1, -10, 10, nameof(AgilityModifier)));
        DecrementAgilityCommand = new Command(() => ChangeStat(ref _agilityModifier, -1, -10, 10, nameof(AgilityModifier)));
        IncrementMindCommand = new Command(() => ChangeStat(ref _mindModifier, 1, -10, 10, nameof(MindModifier)));
        DecrementMindCommand = new Command(() => ChangeStat(ref _mindModifier, -1, -10, 10, nameof(MindModifier)));
        IncrementSpiritCommand = new Command(() => ChangeStat(ref _spiritModifier, 1, -10, 10, nameof(SpiritModifier)));
        DecrementSpiritCommand = new Command(() => ChangeStat(ref _spiritModifier, -1, -10, 10, nameof(SpiritModifier)));

        IncrementSkillBonusCommand = new Command(() => ChangeSkillBonus(1));
        DecrementSkillBonusCommand = new Command(() => ChangeSkillBonus(-1));

        IncrementQuantityCommand = new Command(() => ChangeQuantity(1));
        DecrementQuantityCommand = new Command(() => ChangeQuantity(-1));

        Task.Run(LoadInitialData);
    }

    private void ChangeStat(ref int field, int delta, int min, int max, string propertyName)
    {
        int newValue = field + delta;
        if (newValue < min) newValue = min;
        if (newValue > max) newValue = max;
        field = newValue;
        OnPropertyChanged(propertyName);
    }

    private void ChangeSkillBonus(int delta)
    {
        int newValue = SelectedSkillBonus + delta;
        if (newValue < -5) newValue = -5;
        if (newValue > 5) newValue = 5;
        SelectedSkillBonus = newValue;
    }

    private void ChangeQuantity(int delta)
    {
        int newValue = SelectedStartingItemQuantity + delta;
        if (newValue < 1) newValue = 1;
        if (newValue > 999) newValue = 999;
        SelectedStartingItemQuantity = newValue;
    }

    private async Task LoadInitialData()
    {
        await LoadAllBackgrounds();
        await LoadAvailableItems();
    }

    private async Task LoadAllBackgrounds()
    {
        try
        {
            AllBackgrounds = await _backgroundDataService.GetAllBackgroundsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading backgrounds: {ex.Message}");
        }
    }

    private async Task LoadAvailableItems()
    {
        try
        {
            var items = await _itemDataService.GetAllItemsAsync();
            AvailableItems = items.OrderBy(i => i.Name).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            AvailableItems = new List<Item>();
        }
    }

    private async void CloneFromBackground(CharacterBackground source)
    {
        Name = source.Name;
        Description = source.Description;
        VigorModifier = source.VigorModifier;
        AgilityModifier = source.AgilityModifier;
        MindModifier = source.MindModifier;
        SpiritModifier = source.SpiritModifier;

        SkillBonuses.Clear();
        foreach (var bonus in source.SkillBonuses)
        {
            SkillBonuses.Add(new BGSkillBonuses { SkillName = bonus.SkillName, Bonus = bonus.Bonus });
        }

        StartingItems.Clear();
        if (source.StartingItems != null)
        {
            foreach (var item in source.StartingItems)
            {
                Item? resolvedItem = null;

                if (item.ItemDetails != null)
                {
                    resolvedItem = item.ItemDetails;
                }
                else
                {
                    resolvedItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                }

                if (resolvedItem == null)
                {
                    try
                    {
                        resolvedItem = await _itemDataService.GetItemByIdAsync(item.ItemId);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resolving item {item.ItemId}: {ex.Message}");
                    }
                }

                StartingItems.Add(new StartingItem
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    PlayerNote = item.PlayerNote,
                    ItemDetails = resolvedItem
                });
            }
        }

        // Clone familiar choices
        FamiliarChoices.Clear();
        if (source.FamiliarChoices != null && source.FamiliarChoices.Any())
        {
            HasFamiliarChoices = true;
            foreach (var choice in source.FamiliarChoices)
            {
                var config = new FamiliarChoiceConfig
                {
                    Id = choice.Id,
                    Prompt = choice.Prompt,
                    Description = choice.Description,
                    AllowCustomFamiliar = choice.AllowCustomFamiliar
                };

                // Add filters from the choice
                if (choice.QueryCriteria != null)
                {
                    if (choice.QueryCriteria.AllowedSizes != null)
                    {
                        foreach (var size in choice.QueryCriteria.AllowedSizes)
                        {
                            if (!string.IsNullOrEmpty(size))
                                config.Sizes.Add(new FilterValue { Value = size });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Size) && !config.Sizes.Any(fv => fv.Value == choice.QueryCriteria.Size))
                        config.Sizes.Add(new FilterValue { Value = choice.QueryCriteria.Size });

                    if (choice.QueryCriteria.AllowedIntelligences != null)
                    {
                        foreach (var intel in choice.QueryCriteria.AllowedIntelligences)
                        {
                            if (!string.IsNullOrEmpty(intel))
                                config.Intelligences.Add(new FilterValue { Value = intel });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Intelligence) && !config.Intelligences.Any(fv => fv.Value == choice.QueryCriteria.Intelligence))
                        config.Intelligences.Add(new FilterValue { Value = choice.QueryCriteria.Intelligence });

                    if (choice.QueryCriteria.AllowedSpecies != null)
                    {
                        foreach (var species in choice.QueryCriteria.AllowedSpecies)
                        {
                            if (!string.IsNullOrEmpty(species))
                                config.Species.Add(new FilterValue { Value = species });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Species) && !config.Species.Any(fv => fv.Value == choice.QueryCriteria.Species))
                        config.Species.Add(new FilterValue { Value = choice.QueryCriteria.Species });
                }

                FamiliarChoices.Add(config);
            }
        }
        else
        {
            HasFamiliarChoices = false;
        }

        // Clone item choices
        ItemChoices.Clear();
        if (source.ItemChoices != null && source.ItemChoices.Any())
        {
            HasItemChoices = true;
            foreach (var choice in source.ItemChoices)
            {
                var config = new ItemChoiceConfig
                {
                    Id = choice.Id,
                    Prompt = choice.Prompt,
                    Description = choice.Description,
                    AllowCustomItems = choice.AllowCustomItems,
                    AllowDuplicates = choice.AllowDuplicates,
                    RecommendedMin = choice.RecommendedMin,
                    RecommendedMax = choice.RecommendedMax
                };

                if (choice.QueryCriteria != null)
                {
                    if (choice.QueryCriteria.Category.HasValue)
                        config.Categories.Add(new CategoryFilterValue { Value = choice.QueryCriteria.Category.Value });
                    if (choice.QueryCriteria.AllowedCategories != null)
                    {
                        foreach (var cat in choice.QueryCriteria.AllowedCategories)
                        {
                            if (!config.Categories.Any(cv => cv.Value == cat))
                                config.Categories.Add(new CategoryFilterValue { Value = cat });
                        }
                    }
                }

                ItemChoices.Add(config);
            }
        }
        else
        {
            HasItemChoices = false;
        }

        OnPropertyChanged(nameof(SelectedCloneSource));
        OnPropertyChanged(nameof(StartingItems));
        OnPropertyChanged(nameof(FamiliarChoices));
        OnPropertyChanged(nameof(ItemChoices));
    }

    public async Task LoadBackgroundForEdit(CharacterBackground background)
    {
        System.Diagnostics.Debug.WriteLine($"=== LoadBackgroundForEdit called for: {background.Name} (ID: {background.Id}) ===");

        _editingBackground = background;
        IsEditMode = true;
        SelectedCloneSource = null;

        Name = background.Name;
        Description = background.Description;
        VigorModifier = background.VigorModifier;
        AgilityModifier = background.AgilityModifier;
        MindModifier = background.MindModifier;
        SpiritModifier = background.SpiritModifier;

        SkillBonuses.Clear();
        foreach (var bonus in background.SkillBonuses)
        {
            SkillBonuses.Add(new BGSkillBonuses { SkillName = bonus.SkillName, Bonus = bonus.Bonus });
        }

        StartingItems.Clear();
        if (background.StartingItems != null)
        {
            foreach (var item in background.StartingItems)
            {
                Item? resolvedItem = null;

                if (item.ItemDetails != null)
                {
                    resolvedItem = item.ItemDetails;
                }
                else
                {
                    resolvedItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                }

                if (resolvedItem == null)
                {
                    try
                    {
                        resolvedItem = await _itemDataService.GetItemByIdAsync(item.ItemId);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resolving item {item.ItemId}: {ex.Message}");
                    }
                }

                StartingItems.Add(new StartingItem
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    PlayerNote = item.PlayerNote,
                    ItemDetails = resolvedItem
                });
            }
        }

        // Load familiar choices
        FamiliarChoices.Clear();
        if (background.FamiliarChoices != null && background.FamiliarChoices.Any())
        {
            HasFamiliarChoices = true;
            foreach (var choice in background.FamiliarChoices)
            {
                var config = new FamiliarChoiceConfig
                {
                    Id = choice.Id,
                    Prompt = choice.Prompt,
                    Description = choice.Description,
                    AllowCustomFamiliar = choice.AllowCustomFamiliar
                };

                if (choice.QueryCriteria != null)
                {
                    if (choice.QueryCriteria.AllowedSizes != null)
                    {
                        foreach (var size in choice.QueryCriteria.AllowedSizes)
                        {
                            if (!string.IsNullOrEmpty(size))
                                config.Sizes.Add(new FilterValue { Value = size });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Size) && !config.Sizes.Any(fv => fv.Value == choice.QueryCriteria.Size))
                        config.Sizes.Add(new FilterValue { Value = choice.QueryCriteria.Size });

                    if (choice.QueryCriteria.AllowedIntelligences != null)
                    {
                        foreach (var intel in choice.QueryCriteria.AllowedIntelligences)
                        {
                            if (!string.IsNullOrEmpty(intel))
                                config.Intelligences.Add(new FilterValue { Value = intel });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Intelligence) && !config.Intelligences.Any(fv => fv.Value == choice.QueryCriteria.Intelligence))
                        config.Intelligences.Add(new FilterValue { Value = choice.QueryCriteria.Intelligence });

                    if (choice.QueryCriteria.AllowedSpecies != null)
                    {
                        foreach (var species in choice.QueryCriteria.AllowedSpecies)
                        {
                            if (!string.IsNullOrEmpty(species))
                                config.Species.Add(new FilterValue { Value = species });
                        }
                    }
                    if (!string.IsNullOrEmpty(choice.QueryCriteria.Species) && !config.Species.Any(fv => fv.Value == choice.QueryCriteria.Species))
                        config.Species.Add(new FilterValue { Value = choice.QueryCriteria.Species });
                }

                FamiliarChoices.Add(config);
            }
        }
        else
        {
            HasFamiliarChoices = false;
        }

        // Load item choices
        ItemChoices.Clear();
        if (background.ItemChoices != null && background.ItemChoices.Any())
        {
            HasItemChoices = true;
            foreach (var choice in background.ItemChoices)
            {
                var config = new ItemChoiceConfig
                {
                    Id = choice.Id,
                    Prompt = choice.Prompt,
                    Description = choice.Description,
                    AllowCustomItems = choice.AllowCustomItems,
                    AllowDuplicates = choice.AllowDuplicates,
                    RecommendedMin = choice.RecommendedMin,
                    RecommendedMax = choice.RecommendedMax
                };

                if (choice.QueryCriteria != null)
                {
                    if (choice.QueryCriteria.AllowedCategories != null)
                    {
                        foreach (var cat in choice.QueryCriteria.AllowedCategories)
                        {
                            if (!config.Categories.Any(cv => cv.Value == cat))
                                config.Categories.Add(new CategoryFilterValue { Value = cat });
                        }
                    }
                    if (choice.QueryCriteria.Category.HasValue && !config.Categories.Any(cv => cv.Value == choice.QueryCriteria.Category.Value))
                    {
                        config.Categories.Add(new CategoryFilterValue { Value = choice.QueryCriteria.Category.Value });
                    }
                }

                ItemChoices.Add(config);
            }
        }
        else
        {
            HasItemChoices = false;
        }

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(VigorModifier));
        OnPropertyChanged(nameof(AgilityModifier));
        OnPropertyChanged(nameof(MindModifier));
        OnPropertyChanged(nameof(SpiritModifier));
        OnPropertyChanged(nameof(SkillBonuses));
        OnPropertyChanged(nameof(StartingItems));
        OnPropertyChanged(nameof(FamiliarChoices));
        OnPropertyChanged(nameof(ItemChoices));
        OnPropertyChanged(nameof(HasFamiliarChoices));
        OnPropertyChanged(nameof(HasItemChoices));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ShowCloneSection));
        OnPropertyChanged(nameof(CanSave));

        System.Diagnostics.Debug.WriteLine($"=== LoadBackgroundForEdit complete. IsEditMode: {IsEditMode}, SaveButtonText: {SaveButtonText} ===");
    }

    private void AddSkill()
    {
        if (!CanAddSkill) return;

        if (SkillBonuses.Any(s => s.SkillName == SelectedSkillName))
        {
            var existing = SkillBonuses.First(s => s.SkillName == SelectedSkillName);
            existing.Bonus = SelectedSkillBonus;
        }
        else
        {
            SkillBonuses.Add(new BGSkillBonuses
            {
                SkillName = SelectedSkillName,
                Bonus = SelectedSkillBonus
            });
        }

        SelectedSkillName = string.Empty;
        SelectedSkillBonus = 1;
    }

    private void RemoveSkill(BGSkillBonuses? bonus)
    {
        if (bonus == null) return;
        SkillBonuses.Remove(bonus);
    }

    private void AddStartingItem()
    {
        if (!CanAddStartingItem || SelectedStartingItem == null) return;

        var existing = StartingItems.FirstOrDefault(s => s.ItemId == SelectedStartingItem.Id);
        if (existing != null)
        {
            existing.Quantity += SelectedStartingItemQuantity;
        }
        else
        {
            StartingItems.Add(new StartingItem
            {
                ItemId = SelectedStartingItem.Id,
                Quantity = SelectedStartingItemQuantity,
                ItemDetails = SelectedStartingItem
            });
        }

        SelectedStartingItem = null;
        SelectedStartingItemQuantity = 1;
    }

    private void RemoveStartingItem(StartingItem? item)
    {
        if (item == null) return;
        StartingItems.Remove(item);
    }

    private async Task CreateCustomItemAsync()
    {
        Action<Item> onItemCreated = (newItem) =>
        {
            AvailableItems.Add(newItem);
            AvailableItems = AvailableItems.OrderBy(i => i.Name).ToList();
            SelectedStartingItem = newItem;
            SelectedStartingItemQuantity = 1;
            OnPropertyChanged(nameof(AvailableItems));
            OnPropertyChanged(nameof(CanAddStartingItem));
        };

        var creatorViewModel = new CustomItemCreatorViewModel(
            _itemDataService,
            onItemCreated,
            isLiveGame: false
        );

        var creatorPage = new CustomItemCreatorPage(creatorViewModel);
        await Shell.Current.Navigation.PushModalAsync(creatorPage);
    }

    // ===== FAMILIAR CHOICE METHODS =====
    private void AddFamiliarChoice()
    {
        var config = new FamiliarChoiceConfig
        {
            Id = FamiliarChoices.Count > 0 ? FamiliarChoices.Max(c => c.Id) + 1 : 1,
            Prompt = "Choose a familiar:",
            Description = "",
            AllowCustomFamiliar = true
        };
        FamiliarChoices.Add(config);
        HasFamiliarChoices = true;
    }

    private void RemoveFamiliarChoice(FamiliarChoiceConfig? config)
    {
        if (config == null) return;
        FamiliarChoices.Remove(config);
        if (!FamiliarChoices.Any())
        {
            HasFamiliarChoices = false;
        }
    }

    // NOTE: the old AddFamiliarFilter method (which added an empty string to
    // Sizes, Intelligences, AND Species simultaneously) has been removed entirely.
    // It wasn't wired to anything in the XAML and was dead/buggy leftover code.
    // AddSizeFilter / AddIntelligenceFilter / AddSpeciesFilter below are the real,
    // correctly-scoped equivalents.

    private void RemoveFamiliarFilter(FilterValue? filter)
    {
        if (filter == null) return;

        // Remove by reference - each row is now a distinct wrapper object,
        // so there's no ambiguity even if two rows share the same Value.
        foreach (var config in FamiliarChoices)
        {
            if (config.Sizes.Remove(filter)) { OnPropertyChanged(nameof(FamiliarChoices)); return; }
            if (config.Intelligences.Remove(filter)) { OnPropertyChanged(nameof(FamiliarChoices)); return; }
            if (config.Species.Remove(filter)) { OnPropertyChanged(nameof(FamiliarChoices)); return; }
        }
    }

    private async Task CreateCustomFamiliarAsync(FamiliarChoiceConfig? config)
    {
        if (config == null) return;

        Action<Familiar> onFamiliarCreated = (newFamiliar) =>
        {
            System.Diagnostics.Debug.WriteLine($"Custom familiar created: {newFamiliar.FmlrName}");
        };

        var creatorPage = new CustomFamiliarCreatorPage(_familiarDataService, onFamiliarCreated);
        await Shell.Current.Navigation.PushModalAsync(creatorPage);
    }

    private void AddSizeFilter(FamiliarChoiceConfig? config)
    {
        if (config == null) return;
        config.Sizes.Add(new FilterValue());
        OnPropertyChanged(nameof(FamiliarChoices));
    }

    private void AddIntelligenceFilter(FamiliarChoiceConfig? config)
    {
        if (config == null) return;
        config.Intelligences.Add(new FilterValue());
        OnPropertyChanged(nameof(FamiliarChoices));
    }

    private void AddSpeciesFilter(FamiliarChoiceConfig? config)
    {
        if (config == null) return;
        config.Species.Add(new FilterValue());
        OnPropertyChanged(nameof(FamiliarChoices));
    }

    // ===== ITEM CHOICE METHODS =====
    private void AddItemChoice()
    {
        var config = new ItemChoiceConfig
        {
            Id = ItemChoices.Count > 0 ? ItemChoices.Max(c => c.Id) + 1 : 1,
            Prompt = "Choose an item:",
            Description = "",
            AllowCustomItems = true,
            AllowDuplicates = false,
            RecommendedMin = 1,
            RecommendedMax = 1
        };
        ItemChoices.Add(config);
        HasItemChoices = true;
    }

    private void RemoveItemChoice(ItemChoiceConfig? config)
    {
        if (config == null) return;
        ItemChoices.Remove(config);
        if (!ItemChoices.Any())
        {
            HasItemChoices = false;
        }
    }

    private void AddItemChoiceCategory(ItemChoiceConfig? config)
    {
        if (config == null) return;
        config.Categories.Add(new CategoryFilterValue());
        OnPropertyChanged(nameof(ItemChoices));
    }

    private void RemoveItemChoiceCategory(CategoryFilterValue? category)
    {
        if (category == null) return;

        foreach (var config in ItemChoices)
        {
            if (config.Categories.Remove(category))
            {
                OnPropertyChanged(nameof(ItemChoices));
                break;
            }
        }
    }

    private async Task SaveBackgroundAsync()
    {
        if (!CanSave) return;

        // Convert FamiliarChoiceConfigs to FamiliarChoices for storage
        var familiarChoices = new List<FamiliarChoice>();
        if (HasFamiliarChoices && FamiliarChoices.Any())
        {
            foreach (var config in FamiliarChoices)
            {
                var queryCriteria = new FamiliarQueryCriteria
                {
                    IncludePlayerCreated = true,
                    IncludeFoundation = true
                };

                var sizes = config.Sizes.Where(fv => !string.IsNullOrEmpty(fv.Value)).Select(fv => fv.Value).ToList();
                if (sizes.Any())
                {
                    queryCriteria.AllowedSizes = sizes;
                }

                var intelligences = config.Intelligences.Where(fv => !string.IsNullOrEmpty(fv.Value)).Select(fv => fv.Value).ToList();
                if (intelligences.Any())
                {
                    queryCriteria.AllowedIntelligences = intelligences;
                }

                var species = config.Species.Where(fv => !string.IsNullOrEmpty(fv.Value)).Select(fv => fv.Value).ToList();
                if (species.Any())
                {
                    queryCriteria.AllowedSpecies = species;
                }

                familiarChoices.Add(new FamiliarChoice
                {
                    Id = config.Id,
                    Prompt = config.Prompt,
                    Description = config.Description,
                    RecommendedMin = 1,
                    RecommendedMax = 1,
                    AllowCustomFamiliar = config.AllowCustomFamiliar,
                    QueryCriteria = queryCriteria
                });
            }
        }

        // Convert ItemChoiceConfigs to ItemChoices for storage
        var itemChoices = new List<ItemChoice>();
        if (HasItemChoices && ItemChoices.Any())
        {
            foreach (var config in ItemChoices)
            {
                var validCategories = config.Categories
                    .Where(cv => cv.Value != ItemCategory.Unknown)
                    .Select(cv => cv.Value)
                    .Distinct()
                    .ToList();

                var queryCriteria = new ItemQueryCriteria
                {
                    IncludePlayerCreated = true,
                    IncludeFoundation = true
                };

                if (validCategories.Any())
                {
                    queryCriteria.AllowedCategories = validCategories;

                    if (validCategories.Count == 1)
                    {
                        queryCriteria.Category = validCategories.First();
                    }
                }

                itemChoices.Add(new ItemChoice
                {
                    Id = config.Id,
                    Prompt = config.Prompt,
                    Description = config.Description,
                    RecommendedMin = config.RecommendedMin,
                    RecommendedMax = config.RecommendedMax,
                    AllowDuplicates = config.AllowDuplicates,
                    AllowCustomItems = config.AllowCustomItems,
                    QueryCriteria = queryCriteria
                });
            }
        }

        // Use existing ID when in edit mode, otherwise generate new ID
        int backgroundId;
        if (IsEditMode && _editingBackground != null)
        {
            backgroundId = _editingBackground.Id;
            System.Diagnostics.Debug.WriteLine($"=== Saving in EDIT mode. Using existing ID: {backgroundId} ===");
        }
        else
        {
            backgroundId = await _customBackgroundStorage.GetNextCustomBackgroundIdAsync();
            System.Diagnostics.Debug.WriteLine($"=== Saving in CREATE mode. New ID: {backgroundId} ===");
        }

        var background = new CharacterBackground
        {
            Id = backgroundId,
            Name = Name,
            Description = Description,
            VigorModifier = VigorModifier,
            AgilityModifier = AgilityModifier,
            MindModifier = MindModifier,
            SpiritModifier = SpiritModifier,
            SkillBonuses = SkillBonuses.ToList(),
            StartingItems = new ObservableCollection<StartingItem>(StartingItems),
            FamiliarChoices = familiarChoices,
            ItemChoices = itemChoices
        };

        try
        {
            await _customBackgroundStorage.SaveCustomBackgroundAsync(background);

            Preferences.Default.Set("RefreshBackgrounds", true);

            string message = IsEditMode
                ? $"Background '{Name}' has been updated successfully!"
                : $"Background '{Name}' has been created successfully!";

            await Application.Current.MainPage.DisplayAlertAsync("Success", message, "OK");
            await NavigateBackToBackgroundSelection();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Error",
                $"Failed to save background: {ex.Message}",
                "OK");
        }
    }

    private async Task CancelAsync()
    {
        if (HasUnsavedChanges())
        {
            var confirm = await Application.Current.MainPage.DisplayAlertAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to leave?",
                "Yes, Leave",
                "No, Stay");

            if (!confirm) return;
        }

        await NavigateBackToBackgroundSelection();
    }

    private async Task NavigateBackToBackgroundSelection()
    {
        try
        {
            await Shell.Current.GoToAsync("//CharBuilder_Godrick_BackgroundSelection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            try
            {
                await Shell.Current.GoToAsync("///CharBuilder_Godrick_BackgroundSelection");
            }
            catch
            {
                await Shell.Current.GoToAsync("//CharBuilder_Godrick_BackgroundSelection");
            }
        }
    }

    private bool HasUnsavedChanges()
    {
        return !string.IsNullOrWhiteSpace(Name) ||
               !string.IsNullOrWhiteSpace(Description) ||
               SkillBonuses.Any() ||
               StartingItems.Any() ||
               VigorModifier != 0 ||
               AgilityModifier != 0 ||
               MindModifier != 0 ||
               SpiritModifier != 0 ||
               (HasFamiliarChoices && FamiliarChoices.Any()) ||
               (HasItemChoices && ItemChoices.Any());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ===== FILTER VALUE WRAPPERS =====
// These exist so Picker.SelectedItem can two-way bind to an actual settable
// property (Value) instead of the raw list element itself. Binding
// SelectedItem="{Binding}" directly to a string/enum inside an
// ObservableCollection has no property to write back into, so the picker's
// selection silently never reaches the underlying collection - this was the
// root cause of both the item-category and familiar-filter bugs.
public class FilterValue : INotifyPropertyChanged
{
    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class CategoryFilterValue : INotifyPropertyChanged
{
    private ItemCategory _value = ItemCategory.Unknown;
    public ItemCategory Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ===== FAMILIAR CHOICE CONFIGURATION CLASS =====
public class FamiliarChoiceConfig : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowCustomFamiliar { get; set; } = true;

    private ObservableCollection<FilterValue> _sizes = new();
    private ObservableCollection<FilterValue> _intelligences = new();
    private ObservableCollection<FilterValue> _species = new();

    public ObservableCollection<FilterValue> Sizes
    {
        get => _sizes;
        set { _sizes = value; OnPropertyChanged(); }
    }

    public ObservableCollection<FilterValue> Intelligences
    {
        get => _intelligences;
        set { _intelligences = value; OnPropertyChanged(); }
    }

    public ObservableCollection<FilterValue> Species
    {
        get => _species;
        set { _species = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ===== ITEM CHOICE CONFIGURATION CLASS =====
public class ItemChoiceConfig : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowCustomItems { get; set; } = true;
    public bool AllowDuplicates { get; set; } = false;
    public int RecommendedMin { get; set; } = 1;
    public int RecommendedMax { get; set; } = 1;

    private ObservableCollection<CategoryFilterValue> _categories = new();

    public ObservableCollection<CategoryFilterValue> Categories
    {
        get => _categories;
        set { _categories = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}