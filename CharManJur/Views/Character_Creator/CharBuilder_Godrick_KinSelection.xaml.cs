using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using System.Collections.ObjectModel;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_KinSelection : ContentPage
{
    private const string ThisPageRoute = "///CharBuilder_Godrick_KinSelection";

    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly RaceSelectionViewModel _viewModel;

    private bool _isResetting = false;

    public CharBuilder_Godrick_KinSelection(
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService,
        ICharacterPersistenceService persistenceService,
        RaceSelectionViewModel viewModel)
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

        System.Diagnostics.Debug.WriteLine($"=== KinSelection OnAppearing: NeedsUIReset={_globalMenuDataService.NeedsUIReset} ===");

        // ALWAYS clear the CollectionView first
        RacesCollectionView.SelectedItem = null;
        _viewModel.SelectedRace = null;

        if (_globalMenuDataService.NeedsUIReset == true)
        {
            // RESET PATH
            _isResetting = true;

            var racesList = _viewModel.Races.ToList();
            RacesCollectionView.ItemsSource = null;
            RacesCollectionView.SelectedItem = null;

            await Task.Delay(10);

            RacesCollectionView.ItemsSource = new ObservableCollection<Race>(racesList);
            RacesCollectionView.SelectedItem = null;
            RacesCollectionView.InvalidateMeasure();

            _globalMenuDataService.NeedsUIReset = false;
            _isResetting = false;
        }
        else
        {
            // NORMAL OR LOAD PATH - Restore race from saved data
            _viewModel.RestoreFromSavedData();
        }
    }

    private async void OnCharacterSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CharacterReviewPopup(_charDataService));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharacterBuilderHome");
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
        saveData.PlayerName = _charDataService.PlayerName;
        saveData.CharacterName = _charDataService.CharacterName;
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