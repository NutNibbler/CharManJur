using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using System.Collections.ObjectModel;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_ClassSelection : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly ClassSelectionViewModel _viewModel;

    private bool _isResetting = false;
    private bool _isReturningFromPopup = false;

    public CharBuilder_Godrick_ClassSelection(
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService,
        ICharacterPersistenceService persistenceService,
        ClassSelectionViewModel viewModel)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        _persistenceService = persistenceService;
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Subscribe to the ViewModel's event to know when returning from popup
        _viewModel.SubFeatureSelectionCompleted += OnSubFeatureSelectionCompleted;
    }

    private void OnSubFeatureSelectionCompleted(object? sender, EventArgs e)
    {
        // Set the flag so OnAppearing knows we're returning from the popup
        _isReturningFromPopup = true;
        System.Diagnostics.Debug.WriteLine("=== SubFeatureSelectionCompleted: _isReturningFromPopup = true ===");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine($"=== ClassSelection OnAppearing: NeedsUIReset={_globalMenuDataService.NeedsUIReset}, IsReturningFromPopup={_isReturningFromPopup} ===");

        // If returning from popup, do NOT reload data - preserve the current selection
        if (_isReturningFromPopup)
        {
            System.Diagnostics.Debug.WriteLine("=== Returning from popup - preserving selection ===");
            _isReturningFromPopup = false;
            return;
        }

        // ALWAYS clear selections first
        ClassesCollectionView.SelectedItem = null;
        FeaturesCollectionView.SelectedItem = null;

        if (_globalMenuDataService.NeedsUIReset == true)
        {
            // RESET PATH
            _isResetting = true;

            var classesList = _viewModel.Classes.ToList();
            ClassesCollectionView.ItemsSource = null;
            ClassesCollectionView.SelectedItem = null;

            await Task.Delay(10);

            ClassesCollectionView.ItemsSource = new ObservableCollection<CharacterClass>(classesList);
            ClassesCollectionView.SelectedItem = null;
            ClassesCollectionView.InvalidateMeasure();

            FeaturesCollectionView.SelectedItem = null;

            _globalMenuDataService.NeedsUIReset = false;
            _isResetting = false;
        }
        else
        {
            // NORMAL OR LOAD PATH - Restore class from saved data
            // This runs when the page appears normally OR after loading a character
            _viewModel.RestoreFromSavedData();
        }
    }

    private async void OnCharacterSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CharacterReviewPopup(_charDataService));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_KinSelection");
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
        _charDataService.SetCurrentPage("///CharBuilder_Godrick_ClassSelection");

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