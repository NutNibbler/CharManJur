using CharManJur.ViewModels;

namespace CharManJur.Views;

// Reached with ?visit=<token>&campaignId=<guid>. Shell sets each query value once per visit, so the dashboard
// loads when both have arrived and then forgets them. A campaign is its own session: there is no session id.
[QueryProperty(nameof(Visit), "visit")]
[QueryProperty(nameof(CampaignId), "campaignId")]
public partial class GmDashboardPage : ContentPage
{
    private enum GmPanel { None, Bestow, Players, Viewer, Profiles, Shop, Pad }

    private readonly GmDashboardViewModel _viewModel;
    private GmPanel _openPanel = GmPanel.None;
    private bool _panelBusy;
    private bool _syncingClock;
    private string? _campaignId;
    private string? _visit;
    private string? _loadedVisit;

    public GmDashboardPage(GmDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        EventLog.BindingContext = _viewModel.EventLog;
        BestowPanel.BindingContext = _viewModel.Bestow;
        PlayersPanel.BindingContext = _viewModel.PlayerInterface;
        ViewerPanel.BindingContext = _viewModel.Viewer;
        PadPanel.BindingContext = _viewModel.Pad;
        MapPanel.BindingContext = _viewModel.Map;

        // Leaving the dashboard (or another page opening over it) keeps what was written on the pad or drawn on a map.
        Disappearing += (_, _) => _ = _viewModel.SavePadAsync();

        _viewModel.ClockChanged += SyncClock;

        // Keep the overlay exactly as tall as the area it covers.
        ContentArea.SizeChanged += (_, _) =>
        {
            if (_openPanel != GmPanel.None && !_panelBusy && ContentArea.Height > 0)
                PanelOverlay.HeightRequest = ContentArea.Height;
        };
    }

    // ===== QUERY VALUES =====
    public string? Visit
    {
        get => _visit;
        set { _visit = value; TryLoad(); }
    }

    public string? CampaignId
    {
        get => _campaignId;
        set { _campaignId = value; TryLoad(); }
    }

    private void TryLoad()
    {
        if (string.IsNullOrEmpty(_visit) || string.IsNullOrEmpty(_campaignId)) return;

        string visit = _visit;
        string campaign = _campaignId;
        _visit = null;
        _campaignId = null;

        // Shell hands the same values over again when a page opened from the dashboard (a popup, a bestow page)
        // closes. That isn't a new visit: loading again would empty the player list and close the session.
        if (visit == _loadedVisit)
        {
            System.Diagnostics.Debug.WriteLine($"[GM] Ignored a repeated query for visit {visit}");
            return;
        }

        _loadedVisit = visit;
        _ = LoadAsync(campaign);
    }

    private async Task LoadAsync(string campaignId)
    {
        await HideOverlayInstantlyAsync();
        await _viewModel.LoadAsync(campaignId);
    }

    // ===== CLOCK =====
    private void SyncClock()
    {
        _syncingClock = true;
        try
        {
            int max = _viewModel.HourMax;
            HourSlider.IsEnabled = max > 0;
            HourSlider.Maximum = Math.Max(1, max);   // raised before the value, so the value is never cut short
            HourSlider.Value = Math.Min(_viewModel.Hour, Math.Max(1, max));
        }
        finally
        {
            _syncingClock = false;
        }
    }

    private void OnHourChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_syncingClock) return;
        _viewModel.SetHour((int)Math.Round(e.NewValue));
    }

    private async void OnHourDragCompleted(object? sender, EventArgs e)
    {
        _syncingClock = true;
        HourSlider.Value = Math.Round(HourSlider.Value);   // rests on a whole hour
        _syncingClock = false;

        await _viewModel.CommitHourAsync();
    }

    // ===== SUB-PANELS (slide up over Home) =====
    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        if (_openPanel != GmPanel.None) await TogglePanelAsync(_openPanel);
    }

    private async void OnBestowClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Bestow);
    private async void OnPlayersClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Players);
    private async void OnViewerClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Viewer);
    private async void OnProfilesClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Profiles);
    private async void OnShopClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Shop);
    private async void OnPadClicked(object? sender, EventArgs e) => await TogglePanelAsync(GmPanel.Pad);

    private async void OnClosePanelClicked(object? sender, EventArgs e)
    {
        if (_openPanel != GmPanel.None) await TogglePanelAsync(_openPanel);
    }

    private async Task TogglePanelAsync(GmPanel panel)
    {
        if (_panelBusy) return;
        _panelBusy = true;
        try
        {
            if (_openPanel == panel) await HidePanelAsync();
            else await ShowPanelAsync(panel);
        }
        finally
        {
            _panelBusy = false;
        }
    }

    private async Task ShowPanelAsync(GmPanel panel)
    {
        if (_openPanel == GmPanel.Pad && panel != GmPanel.Pad) await _viewModel.SavePadAsync();

        // Already open on another panel: swap the content in place instead of sliding again.
        bool slideIn = _openPanel == GmPanel.None;

        BestowPanel.IsVisible = panel == GmPanel.Bestow;
        PlayersPanel.IsVisible = panel == GmPanel.Players;
        ViewerPanel.IsVisible = panel == GmPanel.Viewer;
        ProfilesPanel.IsVisible = panel == GmPanel.Profiles;
        ShopPanel.IsVisible = panel == GmPanel.Shop;
        PadPanel.IsVisible = panel == GmPanel.Pad;

        PanelTitleLabel.Text = panel switch
        {
            GmPanel.Bestow => "📦 Bestow",
            GmPanel.Players => "👥 Players",
            GmPanel.Viewer => "👁️ Player Viewer",
            GmPanel.Profiles => "🎭 Profiles",
            GmPanel.Shop => "🏪 Shop",
            GmPanel.Pad => "📝 Pad",
            _ => string.Empty
        };

        PanelOverlay.HeightRequest = ContentArea.Height > 0 ? ContentArea.Height : -1;
        if (slideIn)
        {
            PanelOverlay.TranslationY = Height;   // start below the bottom of the window
            PanelOverlay.IsVisible = true;
        }
        _openPanel = panel;
        if (panel == GmPanel.Bestow) _ = _viewModel.Bestow.RefreshAsync();   // loads once the overlay is visible; newly made custom assets show up
        if (panel == GmPanel.Pad) _ = _viewModel.OpenPadAsync();             // loads once the overlay is visible

        if (slideIn)
        {
            await PanelOverlay.TranslateToAsync(0, 0, 380, Easing.CubicOut);
            SetHomeCovered(true);
        }
    }

    private async Task HidePanelAsync()
    {
        if (_openPanel == GmPanel.Pad) await _viewModel.SavePadAsync();

        SetHomeCovered(false);
        await PanelOverlay.TranslateToAsync(0, Height, 300, Easing.CubicIn);
        PanelOverlay.IsVisible = false;
        _openPanel = GmPanel.None;
    }

    // Used when a campaign loads: no animation, just back to Home.
    private Task HideOverlayInstantlyAsync()
    {
        PanelOverlay.IsVisible = false;
        PanelOverlay.TranslationY = 0;
        _openPanel = GmPanel.None;
        SetHomeCovered(false);
        return Task.CompletedTask;
    }

    // With no skin loaded the overlay has no solid fill, so Home would show through and swallow taps.
    private void SetHomeCovered(bool covered)
    {
        HomeGrid.Opacity = covered ? 0 : 1;
        HomeGrid.InputTransparent = covered;
    }
}