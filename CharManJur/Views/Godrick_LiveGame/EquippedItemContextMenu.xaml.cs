using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class EquippedItemContextMenu : ContentPage
{
    private readonly CharacterItem _characterItem;
    private readonly ICharAttribDataService _charDataService;
    private readonly int _slotNumber;
    private readonly string _slotName;
    private readonly Action _onComplete;
    private readonly CharacterHomePage? _parentPage;
    private readonly Point? _menuPosition;

    public EquippedItemContextMenu(
        CharacterItem characterItem,
        ICharAttribDataService charDataService,
        int slotNumber,
        string slotName,
        Action onComplete,
        CharacterHomePage? parentPage = null,
        Point? menuPosition = null)
    {
        InitializeComponent();

        _characterItem = characterItem;
        _charDataService = charDataService;
        _slotNumber = slotNumber;
        _slotName = slotName;
        _onComplete = onComplete;
        _parentPage = parentPage;
        _menuPosition = menuPosition;

        var viewModel = new EquippedItemContextMenuViewModel(
            characterItem,
            charDataService,
            slotNumber,
            slotName,
            OnInteractionComplete);
        BindingContext = viewModel;
    }

    private void OnInteractionComplete()
    {
        if (_parentPage != null)
        {
            _parentPage.SetReturningFromFlyout(true);
        }
        _onComplete?.Invoke();
    }

    private async void OnDismissClicked(object sender, EventArgs e)
    {
        if (_parentPage != null)
        {
            _parentPage.SetReturningFromFlyout(true);
        }
        await Navigation.PopModalAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // ===== POSITION THE MENU NEAR THE CURSOR =====
        if (_menuPosition.HasValue)
        {
            var pos = _menuPosition.Value;

            // Convert to device-independent units
            var x = pos.X;
            var y = pos.Y;

            // Get screen dimensions
            var screenWidth = Application.Current.MainPage.Width;
            var screenHeight = Application.Current.MainPage.Height;

            // Calculate the menu width (about 200) and height (about 230)
            double menuWidth = 200;
            double menuHeight = 230;

            // Adjust position to keep menu on screen
            if (x + menuWidth > screenWidth)
            {
                x = screenWidth - menuWidth - 10;
            }
            if (x < 10)
            {
                x = 10;
            }
            if (y + menuHeight > screenHeight)
            {
                y = screenHeight - menuHeight - 10;
            }
            if (y < 10)
            {
                y = 10;
            }

            // Apply the position
            MenuBorder.TranslationX = x;
            MenuBorder.TranslationY = y;
        }

        MenuBorder.Opacity = 0;
        MenuBorder.Scale = 0.9;

        this.Animate("MenuAppear", new Animation(
            callback: d =>
            {
                MenuBorder.Opacity = d;
                MenuBorder.Scale = 0.9 + (0.1 * d);
            },
            start: 0,
            end: 1,
            easing: Easing.CubicOut
        ), length: 150);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_parentPage != null)
        {
            _parentPage.SetReturningFromFlyout(true);
        }
    }
}