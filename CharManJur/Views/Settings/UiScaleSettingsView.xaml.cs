using CharManJur.Services;

namespace CharManJur.Views;

public partial class UiScaleSettingsView : ContentView
{
    private bool _updating;

    public UiScaleSettingsView()
    {
        InitializeComponent();
        HelpLabel.Text = UiScale.RestartNeeded
            ? "Scales the whole app, like Windows display scaling. Below 100% fits more on screen (try 60-75% on a phone). " +
              "Takes effect after you restart the app, and you'll be asked to confirm it."
            : "Scales the whole app, like Windows display scaling. Below 100% fits more on screen. " +
              "Applies immediately and asks you to confirm.";
        SetSlider(UiScale.Percent);
    }

    private int Chosen => (int)Math.Round(ScaleSlider.Value);

    private void SetSlider(int value)
    {
        _updating = true;
        ScaleSlider.Value = Math.Clamp(value, UiScale.Min, UiScale.Max);
        ValueLabel.Text = $"{Chosen}%";
        _updating = false;
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_updating) return;
        SetSlider((int)(Math.Round(e.NewValue / UiScale.Step) * UiScale.Step));   // snap to 5%
    }

    private void OnMinusClicked(object? sender, EventArgs e) => SetSlider(Chosen - UiScale.Step);
    private void OnPlusClicked(object? sender, EventArgs e) => SetSlider(Chosen + UiScale.Step);
    private void OnResetClicked(object? sender, EventArgs e) => SetSlider(UiScale.Default);

    private async void OnApplyClicked(object? sender, EventArgs e)
    {
        int value = Chosen;
        if (value == UiScale.Percent) return;

        UiScale.Set(value);
        var page = ContainingPage();

        if (UiScale.RestartNeeded)
        {
            await page.DisplayAlertAsync(
                "Restart needed",
                "Close and reopen the app to apply the new size. You'll be asked to confirm it then.",
                "OK");
        }
        else
        {
            await UiScale.AskKeepAsync(page);
        }

        SetSlider(UiScale.Percent);
    }

    // The page this control sits on (Settings, or the in-game Preferences modal)
    private Page ContainingPage()
    {
        Element? element = this;
        while (element != null && element is not Page) element = element.Parent;
        return element as Page ?? Shell.Current;
    }
}