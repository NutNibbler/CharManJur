using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class VigorSavePopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onResolved;
    private int? _rolledValue;

    public VigorSavePopup(ICharAttribDataService charDataService, Action onResolved)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onResolved = onResolved;

        InstructionLabel.Text = $"Roll 1d20. Succeed if the result is at or under your current Vigor ({charDataService.CurrentStatVigor}). A natural 1 always succeeds; a natural 20 always fails.";
    }

    private void OnRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        ManualEntrySection.IsVisible = ManualRadio.IsChecked;
        RollForMeSection.IsVisible = RollForMeRadio.IsChecked;
    }

    private void OnRollClicked(object sender, EventArgs e)
    {
        _rolledValue = Random.Shared.Next(1, 21);
        RollResultLabel.Text = $"Rolled: {_rolledValue}";
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        int? roll = ManualRadio.IsChecked
            ? (int.TryParse(RollEntry.Text, out int manual) ? manual : (int?)null)
            : _rolledValue;

        if (roll == null || roll < 1 || roll > 20)
        {
            await DisplayAlertAsync("Invalid Roll", "Enter or roll a value from 1 to 20.", "OK");
            return;
        }

        bool success = roll == 1 || (roll != 20 && roll <= _charDataService.CurrentStatVigor);

        _charDataService.ResolveVigorSave(success);

        await DisplayAlertAsync(
            success ? "Save Passed" : "Save Failed",
            success ? "The Vigor Save succeeded." : "The Vigor Save failed — Death's Door applied.",
            "OK");

        _onResolved?.Invoke();
        await Navigation.PopModalAsync();
    }
}