using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class RaceAssetManagerPage : ContentPage
{
    private readonly RaceAssetManagerViewModel _viewModel;

    public RaceAssetManagerPage(RaceAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, race) => await OpenCreator(race);
    }

    private async Task OpenCreator(Race? raceToEdit)
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "ReturnRoute", "//RaceAssetManagerPage" }
        };
        if (raceToEdit != null)
        {
            navigationParameters["RaceToEdit"] = raceToEdit;
        }

        await Shell.Current.GoToAsync("///Godrick_CustomRaceCreator", navigationParameters);
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