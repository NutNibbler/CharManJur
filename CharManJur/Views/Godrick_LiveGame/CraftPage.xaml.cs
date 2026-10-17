using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CraftPage : ContentPage
{
    private readonly CraftViewModel _vm;
    private readonly IItemDataService _itemData;

    // blueprint == null: sandbox crafting.
    public CraftPage(CraftService craft, SubFeatureEntry? blueprint, Action<string> onDone)
    {
        InitializeComponent();
        _itemData = craft.ItemData;
        _vm = new CraftViewModel(craft, blueprint, onDone, () => Navigation.PopModalAsync());
        _vm.CustomItemRequested += OnCustomItemRequested;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();   // loads once
    }

    // Same pattern as the Bestow page's Custom button.
    private async void OnCustomItemRequested(Action<Item> onCreated)
    {
        var creatorViewModel = new CustomItemCreatorViewModel(_itemData, onCreated);
        await Navigation.PushModalAsync(new CustomItemCreatorPage(creatorViewModel));
    }
}