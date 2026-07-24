using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_BackgroundSelection : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly BackgroundSelectionViewModel _viewModel;

    public CharBuilder_Godrick_BackgroundSelection(
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService,
        ICharacterPersistenceService persistenceService,
        BackgroundSelectionViewModel viewModel)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        _persistenceService = persistenceService;
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if we need to refresh using Preferences
        bool refreshNeeded = Preferences.Default.Get("RefreshBackgrounds", false);
        if (refreshNeeded)
        {
            await _viewModel.RefreshBackgroundsAsync();
            Preferences.Default.Set("RefreshBackgrounds", false);
        }
    }

    private async void OnCharacterSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CharacterReviewPopup(_charDataService));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_ClassSelection");
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