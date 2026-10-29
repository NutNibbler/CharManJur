using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class LevelUpPage : ContentPage
{
    private readonly LevelUpViewModel _vm;
    private readonly Func<Task>? _onDone;

    public LevelUpPage(LevelUpService levelUp, ICharAttribDataService data, IItemDataService items, Func<Task>? onDone)
    {
        InitializeComponent();
        _onDone = onDone;

        _vm = new LevelUpViewModel(levelUp, data, items);
        BindingContext = _vm;
        _vm.Finished += OnFinished;

        _ = _vm.InitializeAsync();
    }

    // The level was taken: close, then let the Bio Sheet refresh (it also shows the level-up notice).
    private async void OnFinished()
    {
        await Navigation.PopModalAsync();
        if (_onDone != null) await _onDone();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}