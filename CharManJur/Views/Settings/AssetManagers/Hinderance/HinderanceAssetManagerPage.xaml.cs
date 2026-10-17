using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class HinderanceAssetManagerPage : ContentPage
{
    private readonly HinderanceAssetManagerViewModel _viewModel;

    public HinderanceAssetManagerPage(HinderanceAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, hinderance) => await OpenCreator(hinderance);
    }

    private async Task OpenCreator(Hinderance? hinderanceToEdit)
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "ReturnRoute", "//HinderanceAssetManagerPage" }
        };
        if (hinderanceToEdit != null)
        {
            navigationParameters["HinderanceToEdit"] = hinderanceToEdit;
        }

        await Shell.Current.GoToAsync("///Godrick_CustomHinderanceCreator", navigationParameters);
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