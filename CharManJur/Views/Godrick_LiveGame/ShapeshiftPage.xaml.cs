using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ShapeshiftPage : ContentPage
{
    private readonly ShapeshiftViewModel _vm;

    public ShapeshiftPage(LiveShifter shifter, CharacterClass shifterClass, Action<string> onDone)
    {
        InitializeComponent();
        _vm = new ShapeshiftViewModel(shifter, shifterClass, onDone, () => Navigation.PopModalAsync());
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();   // custom traits and saved forms (loads once)
    }
}