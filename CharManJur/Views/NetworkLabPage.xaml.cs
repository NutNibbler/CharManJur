using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class NetworkLabPage : ContentPage
{
    private readonly NetworkLabViewModel _vm;

    public NetworkLabPage(IGmSessionHub net)
    {
        InitializeComponent();
        _vm = new NetworkLabViewModel(net);
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Stop();
    }

    // Each button sits inside one player's card, so its binding context is that player.
    private static LabPlayerRow? RowOf(object? sender) => (sender as BindableObject)?.BindingContext as LabPlayerRow;

    private async void OnKickClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row) await _vm.KickAsync(row);
    }

    private async void OnLeaveClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row) await _vm.LeaveAsync(row);
    }
}