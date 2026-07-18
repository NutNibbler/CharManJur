using CharManJur.Models;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class CharBuilder_Godrick_SubFeatureSelectionPopup : ContentPage
{
    private readonly SubFeatureSelectionViewModel _viewModel;

    // Event to notify parent when sub-features are selected
    public event EventHandler? SubFeaturesSelected;

    public CharBuilder_Godrick_SubFeatureSelectionPopup(
        SubFeatureSelectionViewModel viewModel,
        SubFeatureType subFeatureType,
        int selectedClassId,
        int selectedFeatureId)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Subscribe to the ViewModel's event
        _viewModel.SubFeaturesConfirmed += OnSubFeaturesConfirmed;

        _viewModel.Initialize(subFeatureType, selectedClassId, selectedFeatureId);
    }

    private void OnSubFeaturesConfirmed(object? sender, EventArgs e)
    {
        // Forward the event to the parent page
        SubFeaturesSelected?.Invoke(this, EventArgs.Empty);

        // Close the popup
        Shell.Current.GoToAsync("..");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine($"=== POPUP OnAppearing: Type={_viewModel.SubFeatureType} ===");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Signal to the parent page that we're returning from the popup
        // The parent page will handle this via the SubFeaturesSelected event
    }
}