using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class Godrick_CustomRaceCreator : ContentPage
{
    private readonly CustomRaceBuilderViewModel _viewModel;

    public Godrick_CustomRaceCreator(
        ICustomRaceStorageService customRaceStorage,
        ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _viewModel = new CustomRaceBuilderViewModel(customRaceStorage, charDataService);
        BindingContext = _viewModel;
    }

    public Godrick_CustomRaceCreator(
        ICustomRaceStorageService customRaceStorage,
        ICharAttribDataService charDataService,
        Models.Race raceToEdit)
        : this(customRaceStorage, charDataService)
    {
        _viewModel.LoadRaceForEdit(raceToEdit);
    }

    // Add this to handle back button press on Android/Windows
    protected override bool OnBackButtonPressed()
    {
        // Let the ViewModel handle the back navigation with unsaved changes check
        _viewModel.CancelCommand.Execute(null);
        return true;
    }
}