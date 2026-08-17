using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_BackgroundSelection : ContentPage
{
    // Kept as a single named constant so Save For Later can't silently point at the
    // wrong page again (this page's handler previously had ClassSelection's route
    // hardcoded here, left over from copy-pasting the ClassSelection page's code).
    private const string ThisPageRoute = "///CharBuilder_Godrick_BackgroundSelection";

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

        _viewModel.RestoreSelectedBackground();
        _viewModel.RestoreSavedLanguages();

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

    private async void OnSaveForLaterClicked(object sender, EventArgs e)
    {
        _charDataService.SetCurrentPage(ThisPageRoute);

        string playerName = string.IsNullOrEmpty(_charDataService.PlayerName)
            ? "UnknownPlayer"
            : _charDataService.PlayerName;

        string characterName = string.IsNullOrEmpty(_charDataService.CharacterName)
            ? "UnknownCharacter"
            : _charDataService.CharacterName;

        string fileName = await _persistenceService.GenerateFileName(playerName, characterName);

        bool fileExists = await _persistenceService.CharacterExistsAsync(fileName);
        if (fileExists)
        {
            bool overrideFile = await DisplayAlertAsync(
                "File Exists",
                $"A character save named '{fileName}' already exists. Override it?",
                "Yes, Override",
                "No, Cancel");

            if (!overrideFile) return;
        }

        var saveData = _charDataService.CreateSaveData();
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;
        saveData.CurrentPage = _charDataService.CurrentPage;

        bool success = await _persistenceService.SaveCharacterDataAsync(saveData);

        if (success)
        {
            _charDataService.MarkCharacterSaved();
            _charDataService.SaveFileName = fileName;

            await DisplayAlertAsync("Character Saved!",
                $"Your character '{characterName}' has been saved.\n" +
                $"Save ID: {fileName}",
                "OK");

            await Shell.Current.GoToAsync("///MainPage");
        }
        else
        {
            await DisplayAlertAsync("Error", "Failed to save character. Please try again.", "OK");
        }
    }

}