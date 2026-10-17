using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class FeatureUsePage : ContentPage
{
    public FeatureUsePage(LiveFeatures live, FeatureEntry entry, Action<string> onDone)
    {
        InitializeComponent();
        BindingContext = new FeatureUseViewModel(live, entry, onDone, () => Navigation.PopModalAsync());
    }
}