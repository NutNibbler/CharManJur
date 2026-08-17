using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

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
                "You have unsaved theme changes.",
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