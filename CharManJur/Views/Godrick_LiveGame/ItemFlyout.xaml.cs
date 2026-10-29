using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Helpers;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ItemFlyout : ContentPage
{
    private readonly CharacterHomePage? _parentPage;
    private readonly Action _onComplete;
    private readonly CharacterItem _item;
    private readonly ICharAttribDataService _charDataService;

    public ItemFlyout(CharacterItem characterItem, ICharAttribDataService charDataService, Action onComplete, CharacterHomePage? parentPage = null)
    {
        InitializeComponent();

        _parentPage = parentPage;
        _onComplete = onComplete;
        _item = characterItem;
        _charDataService = charDataService;
        RollUsageDieHost.IsVisible = RollUsageDieButton.IsVisible = characterItem.HasUsageDie;
        // Worn items (Jewelry, Trinkets) are worn, not put in hands, on the belt or as armor
        bool worn = characterItem.IsWorn;
        EquipHandHost.IsVisible = EquipBeltHost.IsVisible = EquipArmorHost.IsVisible = !worn;
        WearHost.IsVisible = WearButton.IsVisible = worn && !characterItem.IsEquipped;

        // ===== CREATE VIEW MODEL WITH DYNAMIC SLOT SUPPORT =====
        var viewModel = new ItemInteractionViewModel(characterItem, charDataService, OnInteractionComplete);
        BindingContext = viewModel;
        bool custom = characterItem.Template?.IsPlayerCreated == true;
        EditCustomHost.IsVisible = custom;
        EditCustomButton.IsVisible = custom;
    }

    private void OnInteractionComplete()
    {
        if (_parentPage != null)
        {
            _parentPage.SetReturningFromFlyout(true);
        }
        _onComplete?.Invoke();
    }

    private async void OnRollUsageDieClicked(object sender, EventArgs e)
    {
        var outcome = await UsageDieRollHelper.RollAsync(this, _charDataService, _item);
        if (outcome == null) return;   // cancelled

        OnInteractionComplete();       // reloads the dashboard
        await Navigation.PopModalAsync();
    }

    private async void OnWearClicked(object sender, EventArgs e)
    {
        _charDataService.EquipItem(_item.Id, 0);   // worn items ignore the slot
        OnInteractionComplete();
        await Navigation.PopModalAsync();
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

    private async void OnEditCustomClicked(object sender, EventArgs e)
    {
        var template = _item.Template;
        if (template == null) return;

        var items = IPlatformApplication.Current!.Services.GetRequiredService<IItemDataService>();

        // Close the flyout first so the creator opens over the inventory, then refresh it when the edit lands
        await Navigation.PopModalAsync();
        await InGameItemEditor.EditAsync(_charDataService, items, template, OnInteractionComplete);
    }
}