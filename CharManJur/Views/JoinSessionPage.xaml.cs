using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class JoinSessionPage : ContentPage
{
    private readonly JoinSessionViewModel _viewModel;

    public JoinSessionPage(JoinSessionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Stop();
    }

    // The Use button sits inside one found session's row, so its binding context is that session.
    private void OnUseSessionClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is FoundSessionRow row)
            _viewModel.UseSession(row);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MainPage", true);
    }
}