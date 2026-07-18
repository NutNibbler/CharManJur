using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_HinderanceSelection : ContentPage
{
    private readonly HinderanceSelectionViewModel _viewModel;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharAttribDataService _charDataService;

    public CharBuilder_Godrick_HinderanceSelection(
        HinderanceSelectionViewModel viewModel,
        IGlobalMenuDataService globalMenuDataService,
        ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _globalMenuDataService = globalMenuDataService;
        _charDataService = charDataService;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_globalMenuDataService.NeedsUIReset == true)
        {
            // Reset selection if needed
            _viewModel.SelectedHinderance = null;
            _viewModel.SelectedRewardType = HinderanceRewardType.None;
            _viewModel.SelectedStat = null;
            _globalMenuDataService.NeedsUIReset = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_BackgroundSelection");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirmCancel = await DisplayAlertAsync(
            "Cancel New Character?",
            "Are you sure you want to cancel this character? All data will be lost!",
            "Yes",
            "No");

        if (confirmCancel)
        {
            _globalMenuDataService.CharBuilderResetRequest();
            _charDataService.ClearCharacterCreationData();
            await Shell.Current.GoToAsync("///MainPage");
        }
    }
}