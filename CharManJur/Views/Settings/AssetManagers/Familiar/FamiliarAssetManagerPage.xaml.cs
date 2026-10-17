using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.Views;

public partial class FamiliarAssetManagerPage : ContentPage
{
    private readonly IFamiliarDataService _familiarDataService;
    private readonly FamiliarAssetManagerViewModel _viewModel;

    public FamiliarAssetManagerPage(IFamiliarDataService familiarDataService, FamiliarAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _familiarDataService = familiarDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, familiar) => await OpenCreator(familiar);
    }

    private async Task OpenCreator(Familiar? familiarToEdit)
    {
        var creatorPage = new CustomFamiliarCreatorPage(
            _familiarDataService,
            onFamiliarCreated: async (familiar) => await _viewModel.RefreshAsync(),
            familiarToEdit: familiarToEdit);

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