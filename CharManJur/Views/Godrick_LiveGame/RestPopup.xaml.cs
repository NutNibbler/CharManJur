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
        bool deprived = _charDataService.IsDeprived;
        _charDataService.RestoreHitProtection();
        await FinishRest(deprived
            ? "Short rest taken. You're deprived, so Hit Protection was not restored."
            : "Short rest taken. Hit Protection restored.");
    }

    private async void OnLongPerilousClicked(object sender, EventArgs e)
        => await TakeLongRestAsync(LongRestType.Perilous, null);

    private async void OnLongNormalClicked(object sender, EventArgs e)
        => await TakeLongRestAsync(LongRestType.Normal, null);

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

        await TakeLongRestAsync(LongRestType.Comfortable, roll.Value);
    }

    // Every Long Rest grants the Rested chip. A deprived character whose needs aren't met
    // recovers nothing; otherwise the rest recovers normally (and clears deprivation if needs are met).
    private async Task TakeLongRestAsync(LongRestType type, int? roll)
    {
        bool wasDeprived = _charDataService.IsDeprived;
        bool blocked = wasDeprived && !_charDataService.NeedsMet;

        var result = _charDataService.TakeLongRest(type, roll);

        if (blocked)
        {
            await FinishRest($"{type} long rest taken. You gained the Rested chip, but you're deprived and your needs aren't met, so you recover nothing from this rest. " +
                             "Use Daily Needs to meet your needs and you'll no longer be deprived; take another long rest after that to recover.");
            return;
        }

        string message = $"{type} long rest taken. Hit Protection restored.";
        if (type == LongRestType.Normal || type == LongRestType.Comfortable) message += " Fatigue cleared.";
        if (type == LongRestType.Comfortable)
            message += $" Rolled a {result.AbilityScoreRestoreRoll} on a six-sided die — all ability scores restored by that amount.";
        if (wasDeprived && !_charDataService.IsDeprived) message += " You're no longer deprived.";

        await FinishRest(message);
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