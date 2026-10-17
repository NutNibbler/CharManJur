using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class SpellAssetManagerPage : ContentPage
{
    private readonly ISpellDataService _spellDataService;
    private readonly SpellAssetManagerViewModel _viewModel;

    public SpellAssetManagerPage(ISpellDataService spellDataService, SpellAssetManagerViewModel viewModel)
    {
        InitializeComponent();
        _spellDataService = spellDataService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RequestOpenCreator += async (s, e) => await OpenCreator(null);
        _viewModel.RequestOpenEditor += async (s, spell) => await OpenCreator(spell);
    }

    private async Task OpenCreator(Spell? spellToEdit)
    {
        var creatorPage = new CustomSpellCreatorPage(
            _spellDataService,
            onSpellCreated: async (spell) => await _viewModel.RefreshAsync(),
            spellToEdit: spellToEdit);

        await Navigation.PushModalAsync(creatorPage);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "InitialTab", "AssetManager" }
        };
        await Shell.Current.GoToAsync("///SettingsPage", navigationParameters);
    }
}