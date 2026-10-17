using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class MagicCastPage : ContentPage
{
    // spell != null: casting that spell. technique != null: sculpting a cantrip that includes that technique.
    public MagicCastPage(
        MagicCasting casting,
        SubFeatureEntry? spell,
        SubFeatureEntry? technique,
        IEnumerable<SubFeatureEntry> ownedTechniques,
        Action<string> onDone)
    {
        InitializeComponent();
        BindingContext = new MagicCastViewModel(
            casting, spell, technique, ownedTechniques, onDone, () => Navigation.PopModalAsync());
    }
}