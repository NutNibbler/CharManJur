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


    private Item? _itemBeingEdited;
    public bool IsEditMode => _itemBeingEdited != null;

    public CustomItemCreatorViewModel(
    IItemDataService itemDataService,
    Action<Item>? onItemCreated = null,
    bool isLiveGame = false,
    Item? itemToEdit = null)
    {
        _itemDataService = itemDataService;
        _onItemCreated = onItemCreated;
        _isLiveGame = isLiveGame;
        _itemBeingEdited = itemToEdit;

        foreach (WeaponEffectType effect in Enum.GetValues(typeof(WeaponEffectType)))
        {
            bool isSelected = itemToEdit?.WeaponEffects?.Contains(effect) ?? false;
            _weaponEffects.Add(new SelectableWeaponEffect { Effect = effect, IsSelected = isSelected });
        }

        if (itemToEdit != null)
        {
            Name = itemToEdit.Name;
            Description = itemToEdit.BaseDescription;
            Category = itemToEdit.Category;
            Size = itemToEdit.Size;
            ValueInChips = itemToEdit.ValueInChips;
            Rarity = itemToEdit.Rarity;
            QtyLimit = itemToEdit.QtyLimit;
            IsStackable = itemToEdit.IsStackable ?? true;
            Uses = itemToEdit.Uses;
            WeaponCategory = itemToEdit.WeaponCategory;
            WeaponDamage = itemToEdit.WeaponDamage;
            WeaponSpeed = itemToEdit.WeaponSpeed;
            ArmorType = itemToEdit.ArmorType;
            ArmorValue = itemToEdit.ArmorValue;
        }

        CreateCommand = new Command(async () => await SaveItemAsync());
        CancelCommand = new Command(async () => await DismissAsync());
    }

    private async Task SaveItemAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Application.Current.MainPage.DisplayAlertAsync("Error", "Please enter an item name.", "OK");
            return;
        }

        try
        {
            Item savedItem;

            if (_itemBeingEdited != null)
            {
                _itemBeingEdited.Name = Name;
                _itemBeingEdited.BaseDescription = Description;
                _itemBeingEdited.Category = Category;
                _itemBeingEdited.Size = Size;
                _itemBeingEdited.ValueInChips = ValueInChips;
                _itemBeingEdited.Rarity = Rarity;
                _itemBeingEdited.QtyLimit = QtyLimit;
                _itemBeingEdited.IsStackable = IsStackable;
                _itemBeingEdited.Uses = Uses;
                _itemBeingEdited.WeaponCategory = WeaponCategory;
                _itemBeingEdited.WeaponDamage = WeaponDamage;
                _itemBeingEdited.WeaponSpeed = WeaponSpeed;
                _itemBeingEdited.WeaponEffects = WeaponEffects.Where(e => e.IsSelected).Select(e => e.Effect).ToList();
                _itemBeingEdited.ArmorType = ArmorType;
                _itemBeingEdited.ArmorValue = ArmorValue;
                _itemBeingEdited.LastModified = DateTime.UtcNow;

                await _itemDataService.UpdateItemAsync(_itemBeingEdited);
                savedItem = _itemBeingEdited;

                await Application.Current.MainPage.DisplayAlertAsync("Saved!", $"'{savedItem.Name}' updated.", "OK");
            }
            else
            {
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

                savedItem = await _itemDataService.CreateCustomItemAsync(request);
                await Application.Current.MainPage.DisplayAlertAsync("Success!", $"Item '{savedItem.Name}' created with ID {savedItem.Id}", "OK");
            }

            _onItemCreated?.Invoke(savedItem);
            await DismissAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR Saving Item: {ex.Message} ===");
            await Application.Current.MainPage.DisplayAlertAsync("Error", $"Failed to save item: {ex.Message}", "OK");
        }
    }

    private async void OnCancel()
    {
        await DismissAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async Task DismissAsync()
    {
        var mainPage = Application.Current?.MainPage;
        if (mainPage?.Navigation?.ModalStack?.Count > 0)
        {
            await mainPage.Navigation.PopModalAsync();
        }
        else
        {
            await Shell.Current.GoToAsync("..");
        }
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