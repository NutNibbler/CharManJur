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

    public bool CanEquipHand => EquipmentActionHelper.CanEquipToHand(_characterItem, _charDataService);

    public bool CanEquipBelt => EquipmentActionHelper.CanEquipToBelt(_characterItem, _charDataService);

    public bool CanEquipArmor => EquipmentActionHelper.CanEquipAsArmor(_characterItem, _charDataService);

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
            bool success = await EquipmentActionHelper.EquipToHandAsync(_characterItem, _charDataService);
            if (success)
            {
                _onInteractionComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
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
            bool success = await EquipmentActionHelper.EquipToBeltAsync(_characterItem, _charDataService);
            if (success)
            {
                _onInteractionComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
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
            if (await EquipmentActionHelper.EquipAsArmorAsync(_characterItem, _charDataService))
            {
                _onInteractionComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
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
            if (await EquipmentActionHelper.UnequipAsync(_characterItem, _charDataService))
            {
                _onInteractionComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
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
            if (await ItemDropHelper.DropAsync(_characterItem, _charDataService))
            {
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
            if (await ItemTransferHelper.TransferAsync(_characterItem, _charDataService))
            {
                _onInteractionComplete?.Invoke();
                await Application.Current!.MainPage!.Navigation.PopModalAsync();
            }
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