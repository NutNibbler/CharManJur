using CharManJur.Controls;
using CharManJur.ViewModels;

namespace CharManJur.Views;

// The in-game counterpart of Settings: only the Preferences, Theme and Skins tabs. Opens as a modal over the
// character, so the session and character state are untouched.
public partial class PreferencesPage : ContentPage
{
    private enum PreferencesTab { Preferences, Theme, Skins }

    private readonly SettingsViewModel _viewModel;

    public PreferencesPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        SelectTab(PreferencesTab.Preferences);
    }

    private void OnPreferencesTabClicked(object? sender, EventArgs e) => SelectTab(PreferencesTab.Preferences);
    private void OnThemeTabClicked(object? sender, EventArgs e) => SelectTab(PreferencesTab.Theme);
    private void OnSkinsTabClicked(object? sender, EventArgs e) => SelectTab(PreferencesTab.Skins);

    private void SelectTab(PreferencesTab tab)
    {
        PreferencesSection.IsVisible = tab == PreferencesTab.Preferences;
        ThemeSection.IsVisible = tab == PreferencesTab.Theme;
        SkinsSection.IsVisible = tab == PreferencesTab.Skins;

        SetTabLook(PreferencesTabHost, tab == PreferencesTab.Preferences);
        SetTabLook(ThemeTabHost, tab == PreferencesTab.Theme);
        SetTabLook(SkinsTabHost, tab == PreferencesTab.Skins);
    }

    // Selected tab = "primary" art / color, others = "neutral". The fallback color is a dynamic resource,
    // so it still follows live theme edits just like on Settings.
    private static void SetTabLook(SkinHost host, bool selected)
    {
        host.SkinKey = selected ? "primary" : "neutral";
        host.SetDynamicResource(SkinHost.FallbackColorProperty,
            selected ? "ThemePrimaryButtonColor" : "ThemeNeutralButtonColor");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
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

            if (choice == "Save and Go Back") _viewModel.SaveChanges();
            else if (choice == "Discard and Go Back") _viewModel.DiscardChanges();
        }

        await Navigation.PopModalAsync();
    }
}