using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class ItemAssetManagerPage : ContentPage
{
    private readonly IItemDataService _itemDataService;
    private readonly ItemAssetManagerViewModel _viewModel;

    public ItemAssetManagerPage(IItemDataService itemDataService, ItemAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _itemDataService = itemDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, item) => await OpenCreator(item);
    }

    private async Task OpenCreator(Item? itemToEdit)
    {
        var creatorViewModel = new CustomItemCreatorViewModel(
            _itemDataService,
            onItemCreated: async (item) => await _viewModel.RefreshAsync(),
            itemToEdit: itemToEdit);

        var creatorPage = new CustomItemCreatorPage(creatorViewModel);
        await Navigation.PushModalAsync(creatorPage);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///SettingsPage");
    }
}