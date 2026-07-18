using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ItemFlyout : ContentPage
{
    private readonly CharacterHomePage? _parentPage;
    private readonly Action _onComplete;

    public ItemFlyout(CharacterItem characterItem, ICharAttribDataService charDataService, Action onComplete, CharacterHomePage? parentPage = null)
    {
        InitializeComponent();

        _parentPage = parentPage;
        _onComplete = onComplete;

        // ===== CREATE VIEW MODEL WITH DYNAMIC SLOT SUPPORT =====
        var viewModel = new ItemInteractionViewModel(characterItem, charDataService, OnInteractionComplete);
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

        // ===== ANIMATE FLYOUT =====
        FlyoutBorder.Opacity = 0;
        FlyoutBorder.Scale = 0.9;

        this.Animate("FlyoutAppear", new Animation(
            callback: d =>
            {
                FlyoutBorder.Opacity = d;
                FlyoutBorder.Scale = 0.9 + (0.1 * d);
            },
            start: 0,
            end: 1,
            easing: Easing.CubicOut
        ), length: 200);
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