using CharManJur.ViewModels;

namespace CharManJur.Views;

[QueryProperty(nameof(InitialTab), "InitialTab")]
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    // Optional — only overrides SelectedTab's default ("General") when a caller
    // explicitly supplies it, e.g. ItemAssetManagerPage/BackgroundAssetManagerPage
    // passing "AssetManager" so Back returns to the tab the user actually left from.
    public string? InitialTab
    {
        get => _viewModel?.SelectedTab;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && _viewModel != null)
            {
                _viewModel.SelectedTab = value;
            }
        }
    }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (_viewModel.HasUnsavedChanges)
        {
            string choice = await DisplayActionSheetAsync(
                "You have unsaved changes.",
                "Cancel",
                null,
                "Save and Go Back",
                "Discard and Go Back");

            if (choice == "Cancel" || choice == null) return;

            if (choice == "Save and Go Back")
            {
                _viewModel.SaveChanges();
            }
            else if (choice == "Discard and Go Back")
            {
                _viewModel.DiscardChanges();
            }
        }

        await Shell.Current.GoToAsync("///MainPage");
    }
}