using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class TraitAssetManagerPage : ContentPage
{
    private readonly ITraitDataService _traitDataService;
    private readonly TraitAssetManagerViewModel _viewModel;

    public TraitAssetManagerPage(ITraitDataService traitDataService, TraitAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _traitDataService = traitDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, trait) => await OpenCreator(trait);
    }

    private async Task OpenCreator(CustomTrait? traitToEdit)
    {
        var creatorPage = new CustomTraitCreatorPage(
            _traitDataService,
            onSaved: async (trait) => await _viewModel.RefreshAsync(),
            traitToEdit: traitToEdit);

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