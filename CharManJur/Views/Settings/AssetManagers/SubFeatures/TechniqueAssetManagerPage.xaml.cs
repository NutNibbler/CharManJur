using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class TechniqueAssetManagerPage : ContentPage
{
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly TechniqueAssetManagerViewModel _viewModel;

    public TechniqueAssetManagerPage(ITechniqueDataService techniqueDataService, TechniqueAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _techniqueDataService = techniqueDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, technique) => await OpenCreator(technique);
    }

    private async Task OpenCreator(Technique? techniqueToEdit)
    {
        var creatorPage = new CustomTechniqueCreatorPage(
            _techniqueDataService,
            onTechniqueCreated: async (technique) => await _viewModel.RefreshAsync(),
            techniqueToEdit: techniqueToEdit);

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