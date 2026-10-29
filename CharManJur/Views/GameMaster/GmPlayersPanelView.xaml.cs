using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class GmPlayersPanelView : ContentView
{
    public GmPlayersPanelView()
    {
        InitializeComponent();
    }

    // Each button sits inside one player's card, so its binding context is that player.
    private static GmPlayerRow? RowOf(object? sender) => (sender as BindableObject)?.BindingContext as GmPlayerRow;

    private async void OnKickClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.KickAsync(row);
    }

    private async void OnApplyDamageClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.ApplyDamageAsync(row);
    }

    private async void OnApplyEffectClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.ApplyEffectAsync(row);
    }

    private async void OnBestowItemClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.BestowItemAsync(row);
    }

    private async void OnBestowFamiliarClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.BestowFamiliarAsync(row);
    }

    private async void OnBestowSubFeatureClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.BestowSubFeatureAsync(row);
    }

    private async void OnBestowScratchPageClicked(object? sender, EventArgs e)
    {
        if (RowOf(sender) is { } row && BindingContext is GmPlayersViewModel vm) await vm.BestowScratchPageAsync(row);
    }


    // The Kick button on a network player's card: its binding context is that player's row.
    private async void OnKickRemoteClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RemotePlayerRow row && BindingContext is GmPlayersViewModel vm)
            await vm.KickRemoteAsync(row);
    }


    private async void OnSendDamageRemoteClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RemotePlayerRow row && BindingContext is GmPlayersViewModel vm)
            await vm.ApplyDamageRemoteAsync(row);
    }

    private async void OnSendEffectRemoteClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RemotePlayerRow row && BindingContext is GmPlayersViewModel vm)
            await vm.ApplyEffectRemoteAsync(row);
    }


    private async void OnBestowItemRemoteClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RemotePlayerRow row && BindingContext is GmPlayersViewModel vm)
            await vm.BestowItemRemoteAsync(row);
    }

    private async void OnBestowFamiliarRemoteClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RemotePlayerRow row && BindingContext is GmPlayersViewModel vm)
            await vm.BestowFamiliarRemoteAsync(row);
    }
}