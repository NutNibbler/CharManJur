using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class GmBestowPanelView : ContentView
{
    public GmBestowPanelView()
    {
        InitializeComponent();
    }

    // The ✕ on a basket line.
    private void OnRemoveLine(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: GmBestowLine line } && BindingContext is GmBestowViewModel vm)
            vm.RemoveLine(line);
    }
}