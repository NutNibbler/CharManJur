using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class GmViewerPanelView : ContentView
{
    public GmViewerPanelView()
    {
        InitializeComponent();
    }

    private async void OnViewClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is GmPlayerRow row && BindingContext is GmViewerViewModel vm)
            await vm.ViewAsync(row);
    }
}