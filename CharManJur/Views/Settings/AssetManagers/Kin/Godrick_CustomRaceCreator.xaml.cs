using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

[QueryProperty(nameof(RaceToEdit), "RaceToEdit")]
[QueryProperty(nameof(ReturnRoute), "ReturnRoute")]
public partial class Godrick_CustomRaceCreator : ContentPage
{
    private readonly CustomRaceBuilderViewModel _viewModel;
    private Race? _raceToEdit;

    public Race? RaceToEdit
    {
        get => _raceToEdit;
        set
        {
            _raceToEdit = value;
            if (value != null)
            {
                _viewModel?.LoadRaceForEdit(value);
            }
        }
    }

    // Optional — only overrides the ViewModel's default ("//CharBuilder_Godrick_KinSelection")
    // when a caller explicitly supplies it, e.g. a future RaceAssetManagerPage passing
    // "//RaceAssetManagerPage" so Save/Cancel returns there instead.
    public string? ReturnRoute
    {
        get => _viewModel?.ReturnRoute;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && _viewModel != null)
            {
                _viewModel.ReturnRoute = value;
            }
        }
    }

    public Godrick_CustomRaceCreator(
        ICustomRaceStorageService customRaceStorage,
        IRaceDataService raceDataService,
        ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _viewModel = new CustomRaceBuilderViewModel(customRaceStorage, raceDataService, charDataService);
        BindingContext = _viewModel;
    }

    // Add this to handle back button press on Android/Windows
    protected override bool OnBackButtonPressed()
    {
        // Let the ViewModel handle the back navigation with unsaved changes check
        _viewModel.CancelCommand.Execute(null);
        return true;
    }
}