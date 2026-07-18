using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class LoadCharacter : ContentPage
{
    private readonly LoadCharacterViewModel _viewModel;

    public LoadCharacter(LoadCharacterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshCharactersAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MainPage", true);
    }
}