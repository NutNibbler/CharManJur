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
        CancelCommand = new Command(OnCancel);
    }

    public string ItemName => _characterItem.DisplayName;
    public string SlotLocation => $"{_slotName} (Slot {_slotNumber})";
    public bool IsTwoHanded => _characterItem.SlotsRequired > 1;

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
        }
    }

    public ICommand UnequipCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand CancelCommand { get; }

    private async void OnUnequip()
    {
        if (IsProcessing)
        {
            return;
        }
        IsProcessing = true;

        try
        {
            if (_characterItem.SlotsRequired > 1)
            {
                _charDataService.UnequipTwoHandedItem(_characterItem.Id);
            }
            else
            {
                _charDataService.UnequipItem(_characterItem.Id);
            }
            _onComplete?.Invoke();
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void OnDrop()
    {
        if (IsProcessing)
        {
            return;
        }
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
                if (_characterItem.SlotsRequired > 1)
                {
                    _charDataService.UnequipTwoHandedItem(_characterItem.Id);
                }
                _charDataService.DropItem(_characterItem.Id);
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
        if (IsProcessing)
        {
            return;
        }
        IsProcessing = true;

        try
        {
            // Dismiss the context menu
            await Application.Current.MainPage.Navigation.PopModalAsync();

            // Find the CharacterHomePage
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
                    // Find the inventory display item
                    var inventoryItem = viewModel.InventoryItems
                        .FirstOrDefault(i => i.Id == _characterItem.Id);

                    if (inventoryItem != null)
                    {
                        // Set the selected item using the public property
                        viewModel.SelectedInventoryItem = inventoryItem;

                        // Force refresh by raising PropertyChanged on the ViewModel
                        // Since OnPropertyChanged is protected, we use the public setter which triggers it
                        // The SelectedInventoryItem setter already calls OnPropertyChanged internally
                    }
                    else
                    {
                        // If the item is equipped but not in the inventory display,
                        // show details in an alert
                        await Application.Current.MainPage.DisplayAlertAsync(
                            "Item Details",
                            GetItemDetailsString(),
                            "OK");
                    }
                }
            }
            else
            {
                // Fallback: show details in an alert
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

    private string GetItemDetailsString()
    {
        string details = $"Name: {_characterItem.DisplayName}\n";
        details += $"Description: {_characterItem.DisplayDescription}\n";
        details += $"Slot: {_slotName} (Slot {_slotNumber})\n";
        details += $"Slots Required: {_characterItem.SlotsRequired}\n";

        if (_characterItem.SlotsRequired > 1)
        {
            details += "⚔️ Two-Handed (occupies both slots)\n";
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
        if (IsProcessing)
        {
            return;
        }
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}