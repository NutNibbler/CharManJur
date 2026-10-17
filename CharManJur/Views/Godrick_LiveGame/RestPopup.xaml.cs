using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class RestPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onRestTaken;
    private int? _comfortableRollResult;

    public RestPopup(ICharAttribDataService charDataService, Action onRestTaken)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onRestTaken = onRestTaken;
    }

    private async void OnShortRestClicked(object sender, EventArgs e)
    {
        _charDataService.RestoreHitProtection();
        await FinishRest("Short rest taken. Hit Protection restored.");
    }

    private async void OnLongPerilousClicked(object sender, EventArgs e)
    {
        _charDataService.TakeLongRest(LongRestType.Perilous);
        await FinishRest("Perilous long rest taken. Hit Protection restored.");
    }

    private async void OnLongNormalClicked(object sender, EventArgs e)
    {
        _charDataService.TakeLongRest(LongRestType.Normal);
        await FinishRest("Normal long rest taken. Hit Protection restored. Fatigue cleared.");
    }

    private void OnComfortableRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        ComfortableManualSection.IsVisible = ComfortableManualRadio.IsChecked;
        ComfortableRollForMeSection.IsVisible = ComfortableRollForMeRadio.IsChecked;
    }

    private void OnComfortableRollClicked(object sender, EventArgs e)
    {
        _comfortableRollResult = Random.Shared.Next(1, 7);
        ComfortableRollResultLabel.Text = $"Rolled: {_comfortableRollResult}";
    }

    private async void OnLongComfortableClicked(object sender, EventArgs e)
    {
        int? roll = ComfortableManualRadio.IsChecked
            ? (int.TryParse(ComfortableManualEntry.Text, out int manual) ? manual : (int?)null)
            : _comfortableRollResult;

        if (roll == null || roll < 1 || roll > 6)
        {
            await DisplayAlertAsync("Missing Roll", "Enter or roll a d6 result from 1 to 6.", "OK");
            return;
        }

        var result = _charDataService.TakeLongRest(LongRestType.Comfortable, roll.Value);
        await FinishRest($"Comfortable long rest taken. Hit Protection restored. Fatigue cleared. Rolled a {result.AbilityScoreRestoreRoll} on a six-sided die — all ability scores restored by that amount.");
    }

    private async Task FinishRest(string message)
    {
        await DisplayAlertAsync("Rest Complete", message, "OK");
        _onRestTaken?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}