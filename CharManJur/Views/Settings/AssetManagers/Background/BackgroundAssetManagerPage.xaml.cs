using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class BackgroundAssetManagerPage : ContentPage
{
    private readonly BackgroundAssetManagerViewModel _viewModel;

    public BackgroundAssetManagerPage(BackgroundAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, background) => await OpenCreator(background);
    }

    private async Task OpenCreator(CharacterBackground? backgroundToEdit)
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "ReturnRoute", "//BackgroundAssetManagerPage" }
        };
        if (backgroundToEdit != null)
        {
            navigationParameters["BackgroundToEdit"] = backgroundToEdit;
        }

        await Shell.Current.GoToAsync("///Godrick_CustomBackgroundCreator", navigationParameters);
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