using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels;

public class CustomItemCreatorViewModel : INotifyPropertyChanged
{
    private readonly IItemDataService _itemDataService;
    private readonly Action<Item>? _onItemCreated;
    private readonly bool _isLiveGame;  // ← ADDED

    // === FORM FIELDS ===
    private string _name = string.Empty;
    private string? _description;
    private ItemCategory? _category;
    private ItemSize _size = ItemSize.Regular;
    private int? _valueInChips;
    private int? _rarity = 1;
    private int? _qtyLimit;
    private bool _isStackable = true;
    private int? _uses;

    // === WEAPON FIELDS ===
    private WeaponCategoryType? _weaponCategory;
    private WeaponDamageDie? _weaponDamage;
    private WeaponSpeedType? _weaponSpeed;
    private ObservableCollection<SelectableWeaponEffect> _weaponEffects = new();

    // === ARMOR FIELDS ===
    private ArmorType? _armorType;
    private int? _armorValue;

    // === PROPERTIES ===
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string? Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public ItemCategory? Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); }
    }

    public ItemSize Size
    {
        get => _size;
        set { _size = value; OnPropertyChanged(); }
    }

    public int? ValueInChips
    {
        get => _valueInChips;
        set { _valueInChips = value; OnPropertyChanged(); }
    }

    public int? Rarity
    {
        get => _rarity;
        set { _rarity = value; OnPropertyChanged(); }
    }

    public int? QtyLimit
    {
        get => _qtyLimit;
        set { _qtyLimit = value; OnPropertyChanged(); }
    }

    public bool IsStackable
    {
        get => _isStackable;
        set { _isStackable = value; OnPropertyChanged(); }
    }

    public int? Uses
    {
        get => _uses;
        set { _uses = value; OnPropertyChanged(); }
    }

    // === WEAPON PROPERTIES ===
    public WeaponCategoryType? WeaponCategory
    {
        get => _weaponCategory;
        set { _weaponCategory = value; OnPropertyChanged(); }
    }

    public WeaponDamageDie? WeaponDamage
    {
        get => _weaponDamage;
        set { _weaponDamage = value; OnPropertyChanged(); }
    }

    public WeaponSpeedType? WeaponSpeed
    {
        get => _weaponSpeed;
        set { _weaponSpeed = value; OnPropertyChanged(); }
    }

    public ObservableCollection<SelectableWeaponEffect> WeaponEffects
    {
        get => _weaponEffects;
        set { _weaponEffects = value; OnPropertyChanged(); }
    }

    // === ARMOR PROPERTIES ===
    public ArmorType? ArmorType
    {
        get => _armorType;
        set { _armorType = value; OnPropertyChanged(); }
    }

    public int? ArmorValue
    {
        get => _armorValue;
        set { _armorValue = value; OnPropertyChanged(); }
    }

    // === AVAILABLE ENUM LISTS ===
    public List<string> Categories { get; } = Enum.GetNames(typeof(ItemCategory)).ToList();
    public List<string> Sizes { get; } = Enum.GetNames(typeof(ItemSize)).ToList();
    public List<string> RarityLevels { get; } = new List<string> { "1 - Common", "2 - Uncommon", "3 - Rare" };
    public List<string> WeaponCategories { get; } = Enum.GetNames(typeof(WeaponCategoryType)).ToList();
    public List<string> WeaponDamageDice { get; } = Enum.GetNames(typeof(WeaponDamageDie)).ToList();
    public List<string> WeaponSpeeds { get; } = Enum.GetNames(typeof(WeaponSpeedType)).ToList();
    public List<string> ArmorTypes { get; } = Enum.GetNames(typeof(ArmorType)).ToList();

    // === COMMANDS ===
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public CustomItemCreatorViewModel(
        IItemDataService itemDataService,
        Action<Item>? onItemCreated = null,
        bool isLiveGame = false)
    {
        _itemDataService = itemDataService;
        _onItemCreated = onItemCreated;
        _isLiveGame = isLiveGame;

        // Initialize weapon effects
        foreach (WeaponEffectType effect in Enum.GetValues(typeof(WeaponEffectType)))
        {
            _weaponEffects.Add(new SelectableWeaponEffect { Effect = effect, IsSelected = false });
        }

        CreateCommand = new Command(async () => await CreateItemAsync());
        CancelCommand = new Command(OnCancel);
    }

    private async Task CreateItemAsync()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Application.Current.MainPage.DisplayAlertAsync("Error", "Please enter an item name.", "OK");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"=== Creating Item ===");
        System.Diagnostics.Debug.WriteLine($"Name: {Name}");
        System.Diagnostics.Debug.WriteLine($"Category: {Category}");
        System.Diagnostics.Debug.WriteLine($"Size: {Size}");
        System.Diagnostics.Debug.WriteLine($"IsLiveGame: {_isLiveGame}");

        var request = new CreateCustomItemRequest
        {
            Name = Name,
            Category = Category,
            BaseDescription = Description,
            Size = Size,
            ValueInChips = ValueInChips,
            Rarity = Rarity,
            QtyLimit = QtyLimit,
            IsStackable = IsStackable,
            Uses = Uses,
            WeaponCategory = WeaponCategory,
            WeaponDamage = WeaponDamage,
            WeaponSpeed = WeaponSpeed,
            WeaponEffects = WeaponEffects.Where(e => e.IsSelected).Select(e => e.Effect).ToList(),
            ArmorType = ArmorType,
            ArmorValue = ArmorValue,
            IsPlayerCreated = true
        };

        try
        {
            var newItem = await _itemDataService.CreateCustomItemAsync(request);
            System.Diagnostics.Debug.WriteLine($"=== Item Created Successfully! ID: {newItem.Id} ===");

            _onItemCreated?.Invoke(newItem);

            await Application.Current.MainPage.DisplayAlertAsync("Success!",
                $"Item '{newItem.Name}' created with ID {newItem.Id}", "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR Creating Item: {ex.Message} ===");
            await Application.Current.MainPage.DisplayAlertAsync("Error", $"Failed to create item: {ex.Message}", "OK");
        }
    }

    private async void OnCancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SelectableWeaponEffect : INotifyPropertyChanged
{
    private bool _isSelected;

    public WeaponEffectType Effect { get; set; }
    public string DisplayName => Effect.GetDisplayName();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}