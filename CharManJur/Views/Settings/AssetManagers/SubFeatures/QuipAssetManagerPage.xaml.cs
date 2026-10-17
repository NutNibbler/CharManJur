using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class QuipAssetManagerPage : ContentPage
{
    private readonly IQuipDataService _quipDataService;
    private readonly QuipAssetManagerViewModel _viewModel;

    public QuipAssetManagerPage(IQuipDataService quipDataService, QuipAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _quipDataService = quipDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, quip) => await OpenCreator(quip);
    }

    private async Task OpenCreator(Quip? quipToEdit)
    {
        var creatorPage = new CustomQuipCreatorPage(
            _quipDataService,
            onQuipCreated: async (quip) => await _viewModel.RefreshAsync(),
            quipToEdit: quipToEdit);

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