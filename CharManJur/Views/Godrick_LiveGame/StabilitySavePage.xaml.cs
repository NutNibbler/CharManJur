using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class StabilitySavePage : ContentPage
{
    private readonly LiveShifter _shifter;
    private readonly Func<Task>? _afterClose;
    private int? _rolled;

    // afterClose runs once this page has closed (the damage page closes itself then).
    public StabilitySavePage(LiveShifter shifter, Func<Task>? afterClose = null)
    {
        InitializeComponent();
        _shifter = shifter;
        _afterClose = afterClose;

        int stability = shifter.State?.StabilityCurrent ?? 0;
        InstructionLabel.Text = $"Roll 1d20. Succeed if the result is at or under your current Stability ({stability}). " +
                                "A natural 1 always succeeds; a natural 20 always fails. A failed save ends your form.";
    }

    private void OnRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        ManualEntrySection.IsVisible = ManualRadio.IsChecked;
        RollForMeSection.IsVisible = RollForMeRadio.IsChecked;
    }

    private void OnRollClicked(object sender, EventArgs e)
    {
        _rolled = Random.Shared.Next(1, 21);
        RollResultLabel.Text = $"Rolled: {_rolled}";
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        int? roll = ManualRadio.IsChecked
            ? (int.TryParse(RollEntry.Text, out int manual) ? manual : (int?)null)
            : _rolled;

        if (roll == null || roll < 1 || roll > 20)
        {
            await DisplayAlertAsync("Invalid Roll", "Enter or roll a value from 1 to 20.", "OK");
            return;
        }

        var outcome = await _shifter.ResolveSaveAsync(roll.Value);
        await DisplayAlertAsync(outcome.Passed ? "Save Passed" : "Save Failed", outcome.Message, "OK");
        await CloseAsync();
    }

    private async void OnLaterClicked(object sender, EventArgs e) => await CloseAsync();

    private async Task CloseAsync()
    {
        await Navigation.PopModalAsync();
        if (_afterClose != null) await _afterClose();
    }
}