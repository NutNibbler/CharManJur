using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// Rests, daily needs and passing a day on one page. It stays open after each action.
public partial class UpkeepPage : ContentPage
{
    private readonly ICharAttribDataService _svc;
    private readonly Func<Task<FeatureUseResult>>? _passDay;
    private readonly Func<Task> _onChanged;
    private int? _comfortableRollResult;

    public UpkeepPage(ICharAttribDataService svc, Func<Task<FeatureUseResult>>? passDay, Func<Task> onChanged)
    {
        InitializeComponent();
        _svc = svc;
        _passDay = passDay;
        _onChanged = onChanged;

        // In a hosted session only the GM passes the day.
        PassDayPanel.IsVisible = !DailyNeedsActions.GmHosted;

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        NeedsLabel.Text = _svc.NeedsMet ? "🍖 Needs met" : "🍖 Needs not met";
        RestedLabel.Text = _svc.Rested ? "🛏️ Rested" : "🛏️ Not rested";

        DeprivedBanner.IsVisible = _svc.IsDeprived;
        DeprivedLabel.Text = _svc.DeprivedDays > 1 ? $"DEPRIVED ⓘ  ·  day {_svc.DeprivedDays}" : "DEPRIVED ⓘ";
    }

    private async Task ChangedAsync()
    {
        await _onChanged();
        RefreshStatus();
    }

    // ===== RESTS =====
    private async void OnShortRestClicked(object sender, EventArgs e)
    {
        bool deprived = _svc.IsDeprived;
        _svc.RestoreHitProtection();

        await DisplayAlertAsync("Rest Complete", deprived
            ? "Short rest taken. You're deprived, so Hit Protection was not restored."
            : "Short rest taken. Hit Protection restored.", "OK");
        await ChangedAsync();
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

        _comfortableRollResult = null;
        ComfortableRollResultLabel.Text = string.Empty;
        ComfortableManualEntry.Text = string.Empty;
    }

    // Every Long Rest grants the Rested chip. A deprived character whose needs aren't met
    // recovers nothing; otherwise the rest recovers normally (and clears deprivation if needs are met).
    private async Task TakeLongRestAsync(LongRestType type, int? roll)
    {
        bool wasDeprived = _svc.IsDeprived;
        bool blocked = wasDeprived && !_svc.NeedsMet;

        var result = _svc.TakeLongRest(type, roll);

        string message;
        if (blocked)
        {
            message = $"{type} long rest taken. You gained the Rested chip, but you're deprived and your needs aren't met, so you recover nothing from this rest. " +
                      "Use Daily Needs to meet your needs and you'll no longer be deprived; take another long rest after that to recover.";
        }
        else
        {
            message = $"{type} long rest taken. Hit Protection restored.";
            if (type == LongRestType.Normal || type == LongRestType.Comfortable) message += " Fatigue cleared.";
            if (type == LongRestType.Comfortable)
                message += $" Rolled a {result.AbilityScoreRestoreRoll} on a six-sided die — all ability scores restored by that amount.";
            if (wasDeprived && !_svc.IsDeprived) message += " You're no longer deprived.";
        }

        await DisplayAlertAsync("Rest Complete", message, "OK");
        await ChangedAsync();
    }

    // ===== DAILY NEEDS / DAYS =====
    private async void OnDailyNeedsClicked(object sender, EventArgs e)
    {
        if (await DailyNeedsActions.AffirmNeedsAsync(_svc)) await ChangedAsync();
    }

    private async void OnPassDayClicked(object sender, EventArgs e)
    {
        if (await DailyNeedsActions.PassDayAsync(_svc, _passDay)) await ChangedAsync();
    }

    private async void OnDeprivedTapped(object sender, TappedEventArgs e)
        => await DailyNeedsActions.ExplainDeprivationAsync(_svc);

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();
}