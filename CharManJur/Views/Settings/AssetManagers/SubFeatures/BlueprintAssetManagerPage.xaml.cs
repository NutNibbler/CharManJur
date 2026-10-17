using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class BlueprintAssetManagerPage : ContentPage
{
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly BlueprintAssetManagerViewModel _viewModel;

    public BlueprintAssetManagerPage(IBlueprintDataService blueprintDataService, BlueprintAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _blueprintDataService = blueprintDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, blueprint) => await OpenCreator(blueprint);
    }

    private async Task OpenCreator(Blueprint? blueprintToEdit)
    {
        var creatorPage = new CustomBlueprintCreatorPage(
            _blueprintDataService,
            onBlueprintCreated: async (blueprint) => await _viewModel.RefreshAsync(),
            blueprintToEdit: blueprintToEdit);

        await Navigation.PushModalAsync(creatorPage);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "InitialTab", "AssetManager" }
        };
        await Shell.Current.GoToAsync("///SettingsPage", navigationParameters);
    }
}