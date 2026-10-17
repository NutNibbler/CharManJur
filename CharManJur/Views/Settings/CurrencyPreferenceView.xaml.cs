namespace CharManJur.Views;

// The currency-display preference. It binds to the page's SettingsViewModel, so the choice is held as a
// pending change and only applied by Save Changes (on both the main Settings page and in-game Preferences).
public partial class CurrencyPreferenceView : ContentView
{
    public CurrencyPreferenceView()
    {
        InitializeComponent();
    }
}