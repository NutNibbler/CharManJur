using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views.Godrick_LiveGame;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class CharacterHomeViewModel : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _charDataService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly IItemDataService _itemDataService;

    private ObservableCollection<InventoryItemDisplay> _inventoryItems = new();
    private InventoryItemDisplay? _selectedInventoryItem;
    private bool _isLoading;

    // ===== EQUIPMENT BACKING FIELDS =====
    private List<string> _handSlots = new();
    private List<string> _beltSlots = new();
    private string _equippedArmor = "None";
    private string _equippedHands = "0 / 0";
    private string _equippedBelt = "0 / 4";

    // ===== DESTROY MODE =====
    private bool _isDestroyModeActive = false;

    public CharacterHomeViewModel(
        ICharAttribDataService charDataService,
        ICharacterPersistenceService persistenceService,
        IItemDataService itemDataService)
    {
        _charDataService = charDataService;
        _persistenceService = persistenceService;
        _itemDataService = itemDataService;

        // Initialize hand slots based on limb system
        InitializeHandSlots();

        // ===== NAVIGATION COMMANDS =====
        NavigateToSpellsCommand = new Command(async () => await Shell.Current.GoToAsync("///SpellsPage"));
        NavigateToQuipsCommand = new Command(async () => await Shell.Current.GoToAsync("///QuipsPage"));
        NavigateToBlueprintsCommand = new Command(async () => await Shell.Current.GoToAsync("///BlueprintsPage"));
        NavigateToFeaturesCommand = new Command(async () => await Shell.Current.GoToAsync("///FeaturesPage"));
        NavigateToFamiliarsCommand = new Command(async () => await Shell.Current.GoToAsync("///FamiliarsPage"));
        NavigateToShopCommand = new Command(async () => await Shell.Current.GoToAsync("///ShopPage"));
        QuitCommand = new Command(async () => await OnQuit());

        // ===== INVENTORY INTERACTION COMMANDS =====
        InteractCommand = new Command(OnInteract);
        IncrementUsesCommand = new Command(OnIncrementUses);
        DecrementUsesCommand = new Command(OnDecrementUses);
        BestowCommand = new Command(OnBestow);
        ToggleDestroyModeCommand = new Command(OnToggleDestroyMode);
        EditNoteCommand = new Command(OnEditNote);
    }

    private void InitializeHandSlots()
    {
        int totalSlots = _charDataService.GetTotalLimbSlots();
        _handSlots.Clear();
        for (int i = 0; i < totalSlots; i++)
        {
            _handSlots.Add("Empty");
        }
    }

    // ===== HEADER PROPERTIES =====
    public string CharacterName => _charDataService.CharacterName;
    public string PlayerName => _charDataService.PlayerName;
    public string Languages => "Common";

    public int CurrentHP => _charDataService.Hitpoints ?? 0;
    public int MaxHP => _charDataService.Hitpoints ?? 0;

    // ===== STAT PROPERTIES =====
    public int StatVigor => _charDataService.TotalStatVigor;
    public int StatAgility => _charDataService.TotalStatAgility;
    public int StatMind => _charDataService.TotalStatMind;
    public int StatSpirit => _charDataService.TotalStatSpirit;

    public int ASMStatVigor => _charDataService.TotalASMStatVigor;
    public int ASMStatAgility => _charDataService.TotalASMStatAgility;
    public int ASMStatMind => _charDataService.TotalASMStatMind;
    public int ASMStatSpirit => _charDataService.TotalASMStatSpirit;

    // ===== DYNAMIC HAND SLOT PROPERTIES =====
    public List<string> HandSlots
    {
        get => _handSlots;
        set
        {
            _handSlots = value;
            OnPropertyChanged();
        }
    }

    public int TotalHandSlots => _charDataService.GetTotalLimbSlots();

    // ===== LEGACY HAND SLOT PROPERTIES (for backward compatibility) =====
    public string HandSlot1 => GetHandSlot(0);
    public string HandSlot2 => GetHandSlot(1);
    public string HandSlot3 => GetHandSlot(2);
    public string HandSlot4 => GetHandSlot(3);

    private string GetHandSlot(int index)
    {
        if (index < _handSlots.Count)
            return _handSlots[index];
        return "Empty";
    }

    // ===== BULKY ITEM PROPERTIES FOR HAND SLOTS =====
    public bool HandSlot1Bulky => IsSlotBulky(GetEquippedHandSlot(1));
    public bool HandSlot2Bulky => IsSlotBulky(GetEquippedHandSlot(2));
    public bool HandSlot3Bulky => IsSlotBulky(GetEquippedHandSlot(3));
    public bool HandSlot4Bulky => IsSlotBulky(GetEquippedHandSlot(4));

    public bool HasBulkyInHands
    {
        get
        {
            for (int i = 1; i <= TotalHandSlots; i++)
            {
                if (IsSlotBulky(GetEquippedHandSlot(i)))
                    return true;
            }
            return false;
        }
    }

    public string HandSlotsBulkyLabel
    {
        get
        {
            for (int i = 1; i <= TotalHandSlots; i++)
            {
                var item = GetEquippedHandSlot(i);
                if (item != null && item.SlotsRequired > 1)
                    return $"📦 {item.DisplayName} (takes 2 slots)";
            }
            return string.Empty;
        }
    }

    // ===== BELT SLOT PROPERTIES =====
    public string BeltSlot1
    {
        get => _beltSlots.Count > 0 ? _beltSlots[0] : "Empty";
        set
        {
            if (_beltSlots.Count == 0) _beltSlots.Add("Empty");
            _beltSlots[0] = value;
            OnPropertyChanged();
        }
    }

    public string BeltSlot2
    {
        get => _beltSlots.Count > 1 ? _beltSlots[1] : "Empty";
        set
        {
            while (_beltSlots.Count < 2) _beltSlots.Add("Empty");
            _beltSlots[1] = value;
            OnPropertyChanged();
        }
    }

    public string BeltSlot3
    {
        get => _beltSlots.Count > 2 ? _beltSlots[2] : "Empty";
        set
        {
            while (_beltSlots.Count < 3) _beltSlots.Add("Empty");
            _beltSlots[2] = value;
            OnPropertyChanged();
        }
    }

    public string BeltSlot4
    {
        get => _beltSlots.Count > 3 ? _beltSlots[3] : "Empty";
        set
        {
            while (_beltSlots.Count < 4) _beltSlots.Add("Empty");
            _beltSlots[3] = value;
            OnPropertyChanged();
        }
    }

    public bool BeltSlot1Bulky => IsSlotBulky(GetEquippedBeltSlot(1));
    public bool BeltSlot2Bulky => IsSlotBulky(GetEquippedBeltSlot(2));
    public bool BeltSlot3Bulky => IsSlotBulky(GetEquippedBeltSlot(3));
    public bool BeltSlot4Bulky => IsSlotBulky(GetEquippedBeltSlot(4));

    public bool HasBulkyInBelt => BeltSlot1Bulky || BeltSlot2Bulky || BeltSlot3Bulky || BeltSlot4Bulky;

    public string BeltSlotsBulkyLabel
    {
        get
        {
            for (int i = 1; i <= 4; i++)
            {
                var item = GetEquippedBeltSlot(i);
                if (item != null && item.SlotsRequired > 1)
                    return $"📦 {item.DisplayName} (takes 2 slots)";
            }
            return "📦 Bulky item equipped";
        }
    }

    public string EquippedArmor
    {
        get => _equippedArmor;
        set { _equippedArmor = value; OnPropertyChanged(); }
    }

    public string EquippedHands
    {
        get => _equippedHands;
        set { _equippedHands = value; OnPropertyChanged(); }
    }

    public string EquippedBelt
    {
        get => _equippedBelt;
        set { _equippedBelt = value; OnPropertyChanged(); }
    }

    public int Fatigue => 0;

    public int InventorySlotsTotal
    {
        get
        {
            int baseSlots = 6;
            int vigorASM = _charDataService.TotalASMStatVigor;
            return baseSlots + vigorASM;
        }
    }

    // ===== DESTROY MODE =====
    public bool IsDestroyModeActive
    {
        get => _isDestroyModeActive;
        set
        {
            _isDestroyModeActive = value;
            OnPropertyChanged();
            UpdateDestroyModeState();
        }
    }

    // ===== INVENTORY PROPERTIES =====
    public ObservableCollection<InventoryItemDisplay> InventoryItems
    {
        get => _inventoryItems;
        set
        {
            _inventoryItems = value;
            OnPropertyChanged();
        }
    }

    public InventoryItemDisplay? SelectedInventoryItem
    {
        get => _selectedInventoryItem;
        set
        {
            _selectedInventoryItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(SelectedItemName));
            OnPropertyChanged(nameof(SelectedItemDescription));
            OnPropertyChanged(nameof(SelectedItemDetails));
            OnPropertyChanged(nameof(SelectedItemPlayerNote));
            OnPropertyChanged(nameof(HasPlayerNote));
            OnPropertyChanged(nameof(SelectedItemUses));
            OnPropertyChanged(nameof(HasUses));
            OnPropertyChanged(nameof(SelectedItemRemainingUses));
            OnPropertyChanged(nameof(SelectedItemIsEmpty));
            OnPropertyChanged(nameof(SelectedItemIsEquipped));
        }
    }

    public bool HasSelectedItem => SelectedInventoryItem != null;
    public string SelectedItemName => SelectedInventoryItem?.DisplayName ?? string.Empty;
    public string SelectedItemDescription => SelectedInventoryItem?.Description ?? string.Empty;
    public string SelectedItemDetails => SelectedInventoryItem?.Details ?? string.Empty;
    public string SelectedItemPlayerNote => SelectedInventoryItem?.PlayerNote ?? string.Empty;
    public bool HasPlayerNote => !string.IsNullOrEmpty(SelectedItemPlayerNote);
    public string SelectedItemUses => SelectedInventoryItem?.UsesDisplay ?? string.Empty;
    public bool HasUses => SelectedInventoryItem?.MaxUses > 0;
    public string SelectedItemRemainingUses => SelectedInventoryItem?.RemainingUses.ToString() ?? "0";
    public bool SelectedItemIsEmpty => SelectedInventoryItem?.IsEmpty ?? false;
    public bool SelectedItemIsEquipped => SelectedInventoryItem?.IsEquipped ?? false;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICharAttribDataService CharDataService => _charDataService;

    private bool IsSlotBulky(CharacterItem? item)
    {
        return item != null && item.SlotsRequired > 1;
    }

    private CharacterItem? GetEquippedHandSlot(int slot)
    {
        return _charDataService.GetEquippedHandSlot(slot);
    }

    private CharacterItem? GetEquippedBeltSlot(int slot)
    {
        return _charDataService.GetEquippedBeltSlot(slot);
    }

    // ===== COMMANDS =====
    public ICommand NavigateToSpellsCommand { get; }
    public ICommand NavigateToQuipsCommand { get; }
    public ICommand NavigateToBlueprintsCommand { get; }
    public ICommand NavigateToFeaturesCommand { get; }
    public ICommand NavigateToFamiliarsCommand { get; }
    public ICommand NavigateToShopCommand { get; }
    public ICommand QuitCommand { get; }
    public ICommand InteractCommand { get; }
    public ICommand IncrementUsesCommand { get; }
    public ICommand DecrementUsesCommand { get; }
    public ICommand BestowCommand { get; }
    public ICommand ToggleDestroyModeCommand { get; }
    public ICommand EditNoteCommand { get; }

    // ===== INTERACT COMMAND =====
    private async void OnInteract()
    {
        if (SelectedInventoryItem == null) return;

        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;

        var currentPage = Application.Current.MainPage;
        var homePage = currentPage as Views.Godrick_LiveGame.CharacterHomePage;

        if (homePage == null && currentPage?.Navigation?.ModalStack?.Count > 0)
        {
            homePage = currentPage.Navigation.ModalStack.OfType<Views.Godrick_LiveGame.CharacterHomePage>().FirstOrDefault();
        }

        if (homePage != null)
        {
            homePage.ShowFlyout(characterItem);
        }
        else
        {
            var flyout = new Views.Godrick_LiveGame.ItemFlyout(characterItem, _charDataService, async () =>
            {
                await LoadCharacterDataAsync();
            }, null);
            await Shell.Current.Navigation.PushModalAsync(flyout);
        }
    }

    // ===== EDIT PLAYER NOTE COMMAND =====
    private async void OnEditNote()
    {
        if (SelectedInventoryItem == null) return;

        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;

        string currentNote = characterItem.PlayerNote ?? string.Empty;
        string result = await Application.Current.MainPage.DisplayPromptAsync(
            "Edit Note",
            $"Enter a note for '{characterItem.DisplayName}':",
            "Save",
            "Cancel",
            placeholder: "Add your note here...",
            initialValue: currentNote,
            maxLength: 500);

        if (result != null)
        {
            characterItem.PlayerNote = string.IsNullOrWhiteSpace(result) ? null : result;
            characterItem.LastModified = DateTime.Now;

            SelectedInventoryItem.PlayerNote = characterItem.PlayerNote;
            OnPropertyChanged(nameof(SelectedItemPlayerNote));
            OnPropertyChanged(nameof(HasPlayerNote));

            var displayItem = InventoryItems.FirstOrDefault(i => i.Id == characterItem.Id);
            if (displayItem != null)
            {
                displayItem.PlayerNote = characterItem.PlayerNote;
            }
        }
    }

    // ===== USES ADJUSTMENT COMMANDS =====
    private void OnIncrementUses()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;
        if (characterItem.Template?.Uses == null || characterItem.Template.Uses.Value <= 0) return;

        int maxUses = characterItem.Template.Uses.Value;
        if (characterItem.RemainingUses < maxUses)
        {
            characterItem.RemainingUses++;
            characterItem.IsEmpty = characterItem.RemainingUses <= 0;
            characterItem.LastModified = DateTime.Now;
            RefreshSelectedItemDisplay();
        }
    }

    private void OnDecrementUses()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;
        if (characterItem.Template?.Uses == null || characterItem.Template.Uses.Value <= 0) return;

        if (characterItem.RemainingUses > 0)
        {
            characterItem.RemainingUses--;
            characterItem.IsEmpty = characterItem.RemainingUses <= 0;
            characterItem.LastModified = DateTime.Now;
            RefreshSelectedItemDisplay();
        }
    }

    private void RefreshSelectedItemDisplay()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;

        SelectedInventoryItem.RemainingUses = characterItem.RemainingUses;
        SelectedInventoryItem.IsEmpty = characterItem.IsEmpty;
        SelectedInventoryItem.Details = GetItemDetails(characterItem);

        OnPropertyChanged(nameof(SelectedItemRemainingUses));
        OnPropertyChanged(nameof(SelectedItemIsEmpty));
        OnPropertyChanged(nameof(SelectedItemDetails));
        OnPropertyChanged(nameof(SelectedItemUses));
        OnPropertyChanged(nameof(HasUses));
    }

    // ===== BESTOW COMMAND =====
    private async void OnBestow()
    {
        var bestowPage = new BestowItemPage(_itemDataService, _charDataService, async () =>
        {
            await LoadCharacterDataAsync();
        });
        await Shell.Current.Navigation.PushModalAsync(bestowPage);
    }

    // ===== DESTROY MODE =====
    private void OnToggleDestroyMode()
    {
        IsDestroyModeActive = !IsDestroyModeActive;
    }

    private void UpdateDestroyModeState()
    {
        foreach (var item in InventoryItems)
        {
            item.IsDestroyMode = IsDestroyModeActive;
        }
        OnPropertyChanged(nameof(InventoryItems));
    }

    private async void OnDestroyItem(int characterItemId)
    {
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (characterItem == null) return;

        bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Destroy Item",
            $"Are you sure you want to permanently destroy '{characterItem.DisplayName}'?",
            "Yes, Destroy",
            "Cancel");

        if (confirm)
        {
            _charDataService.RemoveItemFromInventory(characterItemId);
            await LoadCharacterDataAsync();
        }
    }

    // ===== LOAD CHARACTER DATA =====
    public async Task LoadCharacterDataAsync()
    {
        IsLoading = true;
        try
        {
            // Initialize hand slots based on current limb system
            InitializeHandSlots();

            InventoryItems.Clear();

            foreach (var characterItem in _charDataService.Inventory)
            {
                if (characterItem.IsDropped) continue;

                bool isStackable = characterItem.Template?.IsStackableItem ?? false;

                if (isStackable)
                {
                    var existingDisplay = InventoryItems.FirstOrDefault(i =>
                        i.Id == characterItem.Id && i.IsStackable);

                    if (existingDisplay != null)
                    {
                        existingDisplay.Quantity += characterItem.Quantity;
                        continue;
                    }

                    var displayItem = new InventoryItemDisplay
                    {
                        Id = characterItem.Id,
                        DisplayName = characterItem.DisplayName,
                        Description = characterItem.DisplayDescription,
                        Details = GetItemDetails(characterItem),
                        Quantity = characterItem.Quantity,
                        IsEmpty = characterItem.IsEmpty,
                        IsEquipped = characterItem.IsEquipped,
                        EquippedSlot = characterItem.SlotIndex,
                        PlayerNote = characterItem.PlayerNote,
                        MaxUses = characterItem.Template?.Uses ?? 0,
                        RemainingUses = characterItem.RemainingUses,
                        IsDestroyMode = IsDestroyModeActive,
                        DestroyCommand = new Command<int>(OnDestroyItem),
                        IsStackable = true,
                        EquippedSlotType = GetEquipmentSlotType(characterItem)
                    };
                    InventoryItems.Add(displayItem);
                }
                else
                {
                    for (int i = 0; i < characterItem.Quantity; i++)
                    {
                        var displayItem = new InventoryItemDisplay
                        {
                            Id = characterItem.Id,
                            DisplayName = characterItem.DisplayName,
                            Description = characterItem.DisplayDescription,
                            Details = GetItemDetails(characterItem),
                            Quantity = 1,
                            IsEmpty = characterItem.IsEmpty,
                            IsEquipped = characterItem.IsEquipped,
                            EquippedSlot = characterItem.SlotIndex,
                            PlayerNote = characterItem.PlayerNote,
                            MaxUses = characterItem.Template?.Uses ?? 0,
                            RemainingUses = characterItem.RemainingUses,
                            IsDestroyMode = IsDestroyModeActive,
                            DestroyCommand = new Command<int>(OnDestroyItem),
                            IsStackable = false,
                            EquippedSlotType = GetEquipmentSlotType(characterItem)
                        };
                        InventoryItems.Add(displayItem);
                    }
                }
            }

            UpdateEquipmentDisplay();
            OnPropertyChanged(nameof(InventorySlotsUsed));
            OnPropertyChanged(nameof(InventorySlotsTotal));
            OnPropertyChanged(nameof(IsEncumbered));

            // Force refresh stat bindings
            OnPropertyChanged(nameof(StatVigor));
            OnPropertyChanged(nameof(StatAgility));
            OnPropertyChanged(nameof(StatMind));
            OnPropertyChanged(nameof(StatSpirit));
            OnPropertyChanged(nameof(ASMStatVigor));
            OnPropertyChanged(nameof(ASMStatAgility));
            OnPropertyChanged(nameof(ASMStatMind));
            OnPropertyChanged(nameof(ASMStatSpirit));

            if (SelectedInventoryItem != null)
            {
                RefreshSelectedItemDisplay();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetEquipmentSlotType(CharacterItem item)
    {
        if (!item.IsEquipped) return string.Empty;

        return item.EquipmentSlot switch
        {
            EquipmentSlotType.Armor => "Armor",
            EquipmentSlotType.Belt => "Belt",
            EquipmentSlotType.Hand => "Hand",
            _ => string.Empty
        };
    }

    // ===== GET ITEM DETAILS =====
    private string GetItemDetails(CharacterItem item)
    {
        var details = new List<string>();

        if (item.Template?.ValueInChips != null)
            details.Add($"Value: {item.Template.ValueInChips} chips");

        if (item.Template?.WeaponDamage != null)
            details.Add($"Damage: {item.Template.WeaponDamage}");

        if (item.Template?.ArmorValue != null)
            details.Add($"Armor: +{item.Template.ArmorValue}");

        if (item.Template?.Uses != null && item.Template.Uses.Value > 0)
            details.Add($"Uses: {item.RemainingUses}/{item.Template.Uses}");

        return details.Count > 0 ? string.Join(" | ", details) : "No additional details";
    }

    // ===== UPDATE EQUIPMENT DISPLAY =====
    private void UpdateEquipmentDisplay()
    {
        // Update hand slots
        int totalSlots = _charDataService.GetTotalLimbSlots();
        _handSlots.Clear();

        for (int i = 1; i <= totalSlots; i++)
        {
            var item = _charDataService.GetEquippedHandSlot(i);
            _handSlots.Add(item?.DisplayName ?? "Empty");
        }

        // Force refresh of hand slot bindings
        OnPropertyChanged(nameof(HandSlots));
        OnPropertyChanged(nameof(TotalHandSlots));
        OnPropertyChanged(nameof(HandSlot1));
        OnPropertyChanged(nameof(HandSlot2));
        OnPropertyChanged(nameof(HandSlot3));
        OnPropertyChanged(nameof(HandSlot4));

        // Update belt slots
        _beltSlots.Clear();
        for (int i = 1; i <= 4; i++)
        {
            var item = _charDataService.GetEquippedBeltSlot(i);
            _beltSlots.Add(item?.DisplayName ?? "Empty");
        }

        // Update armor
        var armor = _charDataService.GetEquippedArmor();
        EquippedArmor = armor?.DisplayName ?? "None";

        // Update equipped counts
        int usedHandSlots = 0;
        for (int i = 1; i <= totalSlots; i++)
        {
            var item = _charDataService.GetEquippedHandSlot(i);
            if (item != null) usedHandSlots += item.SlotsRequired;
        }
        EquippedHands = $"{usedHandSlots} / {totalSlots}";

        int usedBeltSlots = 0;
        for (int i = 1; i <= 4; i++)
        {
            var item = _charDataService.GetEquippedBeltSlot(i);
            if (item != null) usedBeltSlots += item.SlotsRequired;
        }
        EquippedBelt = $"{usedBeltSlots} / 4";

        // Refresh bulky properties
        OnPropertyChanged(nameof(HandSlot1Bulky));
        OnPropertyChanged(nameof(HandSlot2Bulky));
        OnPropertyChanged(nameof(HandSlot3Bulky));
        OnPropertyChanged(nameof(HandSlot4Bulky));
        OnPropertyChanged(nameof(HasBulkyInHands));
        OnPropertyChanged(nameof(HasBulkyInBelt));
        OnPropertyChanged(nameof(HandSlotsBulkyLabel));
        OnPropertyChanged(nameof(BeltSlotsBulkyLabel));
    }

    // ===== QUIT COMMAND =====
    private async Task OnQuit()
    {
        bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Quit Character?",
            "Are you sure you want to return to the main menu? All character data will be saved.",
            "Yes, Quit",
            "Cancel");

        if (!confirm) return;

        string fileName = await _persistenceService.GenerateFileName(
            _charDataService.PlayerName,
            _charDataService.CharacterName);

        var saveData = _charDataService.CreateSaveData();
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;
        saveData.CurrentPage = "CharacterHomePage";

        await _persistenceService.SaveCharacterDataAsync(saveData);
        _charDataService.MarkCharacterSaved();

        await Shell.Current.GoToAsync("///MainPage");
    }

    // ===== HELPER METHOD =====
    public CharacterItem? GetCharacterItem(int id)
    {
        return _charDataService.Inventory.FirstOrDefault(i => i.Id == id);
    }

    // ===== INVENTORY CAPACITY =====
    public int InventorySlotsUsed
    {
        get
        {
            int totalSlots = 0;
            bool isEncumbered = false;

            foreach (var item in _charDataService.Inventory)
            {
                if (item.IsDropped || item.IsEmpty) continue;
                if (item.Template == null) continue;

                int quantity = item.Quantity;
                int slots = item.Template.CalculateSlotsUsed(quantity);
                totalSlots += slots;

                if (item.Template.IsEncumbering(quantity))
                    isEncumbered = true;
            }

            if (isEncumbered)
                return InventorySlotsTotal + 1;

            return totalSlots;
        }
    }

    public bool IsEncumbered => InventorySlotsUsed > InventorySlotsTotal;

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}