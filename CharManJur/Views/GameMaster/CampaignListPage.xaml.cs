using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class CampaignListPage : ContentPage
{
    private readonly CampaignListViewModel _viewModel;

    public CampaignListPage(CampaignListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MainPage", true);
    }
}