using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

[QueryProperty(nameof(BackgroundToEdit), "BackgroundToEdit")]
public partial class Godrick_CustomBackgroundCreator : ContentPage
{
    private readonly CustomBackgroundBuilderViewModel _viewModel;
    private CharacterBackground? _backgroundToEdit;

    public CharacterBackground? BackgroundToEdit
    {
        get => _backgroundToEdit;
        set
        {
            _backgroundToEdit = value;
            System.Diagnostics.Debug.WriteLine($"=== BackgroundToEdit set: {value?.Name ?? "null"} (ID: {value?.Id ?? 0}) ===");
            if (value != null)
            {
                _viewModel?.LoadBackgroundForEdit(value);
            }
        }
    }

    public Godrick_CustomBackgroundCreator(
        ICustomBackgroundStorageService customBackgroundStorage,
        IBackgroundDataService backgroundDataService,
        IItemDataService itemDataService,
        IFamiliarDataService familiarDataService,
        ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _viewModel = new CustomBackgroundBuilderViewModel(
            customBackgroundStorage,
            backgroundDataService,
            itemDataService,
            familiarDataService,
            charDataService);
        BindingContext = _viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }
}