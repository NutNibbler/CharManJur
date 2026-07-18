using CharManJur.Models;
using CharManJur.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class ItemInteractionViewModel : INotifyPropertyChanged
{
    private readonly CharacterItem _characterItem;
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onInteractionComplete;
    private bool _isProcessing;

    public ItemInteractionViewModel(
        CharacterItem characterItem,
        ICharAttribDataService charDataService,
        Action onInteractionComplete)
    {
        _characterItem = characterItem;
        _charDataService = charDataService;
        _onInteractionComplete = onInteractionComplete;

        EquipHandCommand = new Command(OnEquipHand);
        EquipBeltCommand = new Command(OnEquipBelt);
        EquipArmorCommand = new Command(OnEquipArmor);
        UnequipCommand = new Command(OnUnequip);
        DropCommand = new Command(OnDrop);
        TransferCommand = new Command(OnTransfer);
        CancelCommand = new Command(OnCancel);
    }

    // ===== PROPERTIES FOR BINDING =====

    public string ItemName => _characterItem.DisplayName;
    public string ItemDescription => _characterItem.DisplayDescription;
    public bool IsEquipped => _characterItem.IsEquipped;
    public bool IsEmpty => _characterItem.IsEmpty;
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
        }
    }

    public int TotalHandSlots => _charDataService.GetTotalLimbSlots();
    public int UsedHandSlots => _charDataService.GetUsedHandSlots();
    public int AvailableHandSlots => TotalHandSlots - UsedHandSlots;

    public string SlotInfoDisplay
    {
        get
        {
            if (_characterItem.SlotsRequired > 1)
                return $"📦 Takes {_characterItem.SlotsRequired} slots | Hand slots: {AvailableHandSlots}/{TotalHandSlots}";
            else
                return $"Hand slots: {AvailableHandSlots}/{TotalHandSlots}";
        }
    }

    public bool CanEquipHand
    {
        get
        {
            if (_characterItem.IsJewelry) return false;
            if (_characterItem.IsEquipped) return false;
            if (_characterItem.IsArmor && !_characterItem.IsShield) return false;

            // Check if we have enough hand slots available
            if (_characterItem.SlotsRequired > AvailableHandSlots) return false;

            return true;
        }
    }

    public bool CanEquipBelt
    {
        get
        {
            if (_characterItem.IsJewelry) return false;
            if (_characterItem.IsEquipped) return false;
            if (_characterItem.IsArmor && !_characterItem.IsShield) return false;
            return true;
        }
    }

    public bool CanEquipArmor
    {
        get
        {
            if (_characterItem.IsEmpty) return false;
            if (!_characterItem.IsArmor) return false;
            if (_characterItem.IsShield) return false;
            if (_characterItem.IsEquipped) return false;
            return true;
        }
    }

    // ===== COMMANDS =====

    public ICommand EquipHandCommand { get; }
    public ICommand EquipBeltCommand { get; }
    public ICommand EquipArmorCommand { get; }
    public ICommand UnequipCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand TransferCommand { get; }
    public ICommand CancelCommand { get; }

    // ===== COMMAND HANDLERS =====

    private async void OnEquipHand()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            // ===== TWO-HANDED ITEM EQUIPPING =====
            if (_characterItem.SlotsRequired > 1)
            {
                // Check if the item can be equipped as two-handed
                if (!_charDataService.CanEquipTwoHandedItem(_characterItem.Id))
                {
                    await Application.Current.MainPage.DisplayAlertAsync(
                        "Cannot Equip",
                        "This item requires two hands. No free paired limb set available.",
                        "OK");
                    return;
                }

                // Find available limb sets
                var limbSets = _charDataService.LimbSets
                    .Where(l => l.PairType == LimbPairType.Paired && !l.IsOccupiedByTwoHandedItem)
                    .ToList();

                // Check if all slots are free for each set
                var availableSets = limbSets.Where(l =>
                    l.SlotIndices.All(slotIndex =>
                        _charDataService.GetEquippedHandSlot(slotIndex + 1) == null)).ToList();

                if (availableSets.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlertAsync(
                        "Cannot Equip",
                        "No free paired limb sets available.",
                        "OK");
                    return;
                }

                if (availableSets.Count == 1)
                {
                    // If only one limb set available, use it directly
                    _charDataService.EquipTwoHandedItem(_characterItem.Id, availableSets.First().Id);
                    _onInteractionComplete?.Invoke();
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                    return;
                }

                // Multiple limb sets - let user choose
                var options = availableSets.Select(l => l.DisplayName).ToList();
                options.Add("Cancel");

                string selected = await Application.Current.MainPage.DisplayActionSheetAsync(
                    "Select Limb Set",
                    "Cancel",
                    null,
                    options.ToArray());

                if (selected != null && selected != "Cancel")
                {
                    var selectedSet = availableSets.FirstOrDefault(l => l.DisplayName == selected);
                    if (selectedSet != null)
                    {
                        _charDataService.EquipTwoHandedItem(_characterItem.Id, selectedSet.Id);
                        _onInteractionComplete?.Invoke();
                        await Application.Current.MainPage.Navigation.PopModalAsync();
                    }
                }
                return;
            }

            // ===== SINGLE-HANDED ITEM EQUIPPING =====
            int totalSlots = TotalHandSlots;

            if (AvailableHandSlots < 1)
            {
                await Application.Current.MainPage.DisplayAlertAsync(
                    "Not Enough Slots",
                    $"You have no free hand slots. Available: {AvailableHandSlots}/{TotalHandSlots}",
                    "OK");
                return;
            }

            int targetSlot = -1;
            for (int i = 1; i <= totalSlots; i++)
            {
                if (_charDataService.GetEquippedHandSlot(i) == null)
                {
                    targetSlot = i;
                    break;
                }
            }

            if (targetSlot == -1)
            {
                await Application.Current.MainPage.DisplayAlertAsync(
                    "No Slots Available",
                    "No free hand slots available.",
                    "OK");
                return;
            }

            if (_characterItem.IsEquipped)
            {
                _charDataService.UnequipItem(_characterItem.Id);
            }

            _charDataService.EquipItem(_characterItem.Id, targetSlot);
            _onInteractionComplete?.Invoke();
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnEquipBelt()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            int slotsNeeded = _characterItem.SlotsRequired;

            // Count used belt slots
            int usedSlots = 0;
            for (int i = 1; i <= 4; i++)
            {
                var existing = _charDataService.GetEquippedBeltSlot(i);
                if (existing != null) usedSlots += existing.SlotsRequired;
            }

            if (usedSlots + slotsNeeded > 4)
            {
                await Application.Current.MainPage.DisplayAlertAsync(
                    "Not Enough Slots",
                    $"This item requires {slotsNeeded} belt slot(s). You have {4 - usedSlots} available.",
                    "OK");
                return;
            }

            // Find first available slot
            int targetSlot = -1;
            bool found = false;

            if (slotsNeeded == 1)
            {
                for (int i = 1; i <= 4; i++)
                {
                    if (_charDataService.GetEquippedBeltSlot(i) == null)
                    {
                        targetSlot = i;
                        found = true;
                        break;
                    }
                }
            }
            else if (slotsNeeded == 2)
            {
                // Find two consecutive free slots
                for (int i = 1; i <= 3; i++)
                {
                    if (_charDataService.GetEquippedBeltSlot(i) == null &&
                        _charDataService.GetEquippedBeltSlot(i + 1) == null)
                    {
                        targetSlot = i;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                await Application.Current.MainPage.DisplayAlertAsync(
                    "No Slots Available",
                    "Could not find enough consecutive belt slots.",
                    "OK");
                return;
            }

            // Belt slots start after hand slots
            // If total hand slots = 4, belt slot 1 = slot 5
            int beltStartSlot = TotalHandSlots;
            _charDataService.EquipItem(_characterItem.Id, beltStartSlot + targetSlot);
            _onInteractionComplete?.Invoke();
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnEquipArmor()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            var existingArmor = _charDataService.GetEquippedArmor();
            if (existingArmor != null)
            {
                bool replace = await Application.Current.MainPage.DisplayAlertAsync(
                    "Armor Slot Occupied",
                    $"You already have '{existingArmor.DisplayName}' equipped. Replace it?",
                    "Yes, Replace",
                    "Cancel");

                if (!replace) return;
            }

            _charDataService.EquipItemAsArmor(_characterItem.Id);
            _onInteractionComplete?.Invoke();
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnUnequip()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            _charDataService.UnequipItem(_characterItem.Id);
            _onInteractionComplete?.Invoke();
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnDrop()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
                "Drop Item",
                $"Are you sure you want to drop '{_characterItem.DisplayName}'?",
                "Yes, Drop",
                "Cancel");

            if (confirm)
            {
                _charDataService.DropItem(_characterItem.Id);
                _onInteractionComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnTransfer()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Transfer Not Implemented",
                "Item transfer to other players will be available in a future update.",
                "OK");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnCancel()
    {
        if (IsProcessing) return;
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    // ===== INotifyPropertyChanged =====

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}