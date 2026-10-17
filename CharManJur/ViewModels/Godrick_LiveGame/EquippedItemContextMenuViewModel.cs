using CharManJur.Models;
using CharManJur.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class EquippedItemContextMenuViewModel : INotifyPropertyChanged
{
    private readonly CharacterItem _characterItem;
    private readonly ICharAttribDataService _charDataService;
    private readonly int _slotNumber;
    private readonly string _slotName;
    private readonly Action _onComplete;
    private bool _isProcessing;

    public EquippedItemContextMenuViewModel(
        CharacterItem characterItem,
        ICharAttribDataService charDataService,
        int slotNumber,
        string slotName,
        Action onComplete)
    {
        _characterItem = characterItem;
        _charDataService = charDataService;
        _slotNumber = slotNumber;
        _slotName = slotName;
        _onComplete = onComplete;

        UnequipCommand = new Command(OnUnequip);
        DropCommand = new Command(OnDrop);
        ViewDetailsCommand = new Command(OnViewDetails);
        SwapToBeltCommand = new Command(OnSwapToBelt);
        SwapToHandCommand = new Command(OnSwapToHand);
        CancelCommand = new Command(OnCancel);

        // Update command can-execute states
        (SwapToBeltCommand as Command)?.ChangeCanExecute();
        (SwapToHandCommand as Command)?.ChangeCanExecute();
    }

    public string ItemName => _characterItem.DisplayName;
    public string SlotLocation => _slotName;   // callers already pass a complete label ("Left (Slot 1)", "Belt Slot 2", "Armor")
    public bool IsTwoHanded => _characterItem.SlotsRequired > 1;
    public bool CanSwapToBelt => EquipmentActionHelper.CanEquipToBelt(_characterItem, _charDataService);
    public bool CanSwapToHand => EquipmentActionHelper.CanEquipToHand(_characterItem, _charDataService);

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
        }
    }

    // ===== COMMANDS =====
    public ICommand UnequipCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand SwapToBeltCommand { get; }
    public ICommand SwapToHandCommand { get; }
    public ICommand CancelCommand { get; }

    // ===== COMMAND HANDLERS =====

    private async void OnUnequip()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            // Releases a two-handed item's limb set as well; while the pack is dropped, warns and drops it instead.
            if (await EquipmentActionHelper.UnequipAsync(_characterItem, _charDataService))
            {
                _onComplete?.Invoke();
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
                _onComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnViewDetails()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();

            var currentPage = Application.Current.MainPage;
            var homePage = currentPage as Views.Godrick_LiveGame.CharacterHomePage;

            if (homePage == null && currentPage?.Navigation?.ModalStack?.Count > 0)
            {
                homePage = currentPage.Navigation.ModalStack
                    .OfType<Views.Godrick_LiveGame.CharacterHomePage>()
                    .FirstOrDefault();
            }

            if (homePage != null)
            {
                var viewModel = homePage.BindingContext as CharacterHomeViewModel;
                if (viewModel != null)
                {
                    var inventoryItem = viewModel.InventoryItems
                        .FirstOrDefault(i => i.Id == _characterItem.Id);

                    if (inventoryItem != null)
                    {
                        viewModel.SelectedInventoryItem = inventoryItem;
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlertAsync(
                            "Item Details",
                            GetItemDetailsString(),
                            "OK");
                    }
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlertAsync(
                    "Item Details",
                    GetItemDetailsString(),
                    "OK");
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnSwapToBelt()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            bool success = await EquipmentActionHelper.EquipToBeltAsync(_characterItem, _charDataService);
            if (success)
            {
                _onComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnSwapToHand()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            bool success = await EquipmentActionHelper.EquipToHandAsync(_characterItem, _charDataService);
            if (success)
            {
                _onComplete?.Invoke();
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private string GetItemDetailsString()
    {
        string details = $"Name: {_characterItem.DisplayName}\n";
        details += $"Description: {_characterItem.DisplayDescription}\n";
        details += $"Slot: {_slotName}\n";
        details += $"Slots Required: {_characterItem.SlotsRequired}\n";

        if (_characterItem.SlotsRequired > 1)
        {
            details += "⚔️ Two-Handed (occupies 2 slots)\n";
        }

        if (_characterItem.Template?.ValueInChips != null)
        {
            details += $"Value: {_characterItem.Template.ValueInChips} chips\n";
        }

        if (_characterItem.Template?.WeaponDamage != null)
        {
            details += $"Damage: {_characterItem.Template.WeaponDamage}\n";
        }

        if (_characterItem.Template?.WeaponSpeed != null)
        {
            details += $"Weapon Speed: {_characterItem.Template.WeaponSpeed}\n";
        }

        if (_characterItem.Template?.ArmorValue != null)
        {
            details += $"Armor: +{_characterItem.Template.ArmorValue}\n";
        }

        if (_characterItem.Template?.Uses != null && _characterItem.Template.Uses.Value > 0)
        {
            details += $"Uses: {_characterItem.RemainingUses}/{_characterItem.Template.Uses}\n";
        }

        return details;
    }

    private async void OnCancel()
    {
        if (IsProcessing) return;
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}