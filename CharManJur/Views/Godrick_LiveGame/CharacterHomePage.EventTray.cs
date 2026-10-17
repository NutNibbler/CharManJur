using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CharacterHomePage
{
    private const double TrayWidth = 400;
    private const double TrayOffScreen = TrayWidth + 20;
    private const string TrayPinnedKey = "EventTrayPinned";

    private EventTrayViewModel? _eventTrayVm;
    private bool _trayOpen;
    private bool _trayPinned;
    private bool _trayBusy;

    private void InitializeEventTray()
    {
        var charData = _viewModel.CharDataService;
        var log = IPlatformApplication.Current!.Services.GetRequiredService<IPlayerActionLogService>();

        // Only events about this character; the GM dashboard will pass a different filter.
        _eventTrayVm = new EventTrayViewModel(log, e => e.CharacterGuid == charData.CharacterGuid);
        _eventTrayVm.UnreadChanged += () => EventTrayButton.Text = _eventTrayVm.ButtonText;
        EventTray.BindingContext = _eventTrayVm;

        EventTrayOverlay.TranslationX = TrayOffScreen;

        // A tray the player pinned comes back pinned.
        _trayPinned = Preferences.Default.Get(TrayPinnedKey, false);
        UpdatePinVisuals();
        if (_trayPinned) MainThread.BeginInvokeOnMainThread(async () => await SetTrayOpenAsync(true));
    }

    // Called when a different character is loaded into this page.
    private void ResetEventTray()
    {
        _trayOpen = false;
        EventTrayOverlay.IsVisible = false;
        EventTrayOverlay.TranslationX = TrayOffScreen;
        ApplyTrayDock();
        _eventTrayVm?.Reset();
        if (_eventTrayVm != null) EventTrayButton.Text = _eventTrayVm.ButtonText;

        // A pinned tray stays up for the next character.
        if (_trayPinned) MainThread.BeginInvokeOnMainThread(async () => await SetTrayOpenAsync(true));
    }

    private async void OnEventTrayClicked(object? sender, EventArgs e) => await SetTrayOpenAsync(!_trayOpen);

    private async void OnCloseEventTrayClicked(object? sender, EventArgs e) => await SetTrayOpenAsync(false);

    // Pinning docks the tray: the page makes room for it instead of letting it cover the right edge.
    private void OnPinEventTrayClicked(object? sender, EventArgs e)
    {
        _trayPinned = !_trayPinned;
        Preferences.Default.Set(TrayPinnedKey, _trayPinned);
        UpdatePinVisuals();
        ApplyTrayDock();
    }

    private async Task SetTrayOpenAsync(bool open)
    {
        if (_trayBusy || _eventTrayVm == null || _trayOpen == open) return;
        _trayBusy = true;
        try
        {
            if (open)
            {
                await _viewModel.FlushPendingLogAsync();   // show any pending uses entries
                await _eventTrayVm.OpenAsync();
                EventTrayOverlay.TranslationX = TrayOffScreen;
                EventTrayOverlay.IsVisible = true;
                _trayOpen = true;
                ApplyTrayDock();
                await EventTrayOverlay.TranslateToAsync(0, 0, 320, Easing.CubicOut);
            }
            else
            {
                _trayOpen = false;
                _eventTrayVm.IsOpen = false;

                // Closing the tray also unpins it.
                if (_trayPinned)
                {
                    _trayPinned = false;
                    Preferences.Default.Set(TrayPinnedKey, false);
                    UpdatePinVisuals();
                }

                ApplyTrayDock();
                await EventTrayOverlay.TranslateToAsync(TrayOffScreen, 0, 260, Easing.CubicIn);
                EventTrayOverlay.IsVisible = false;
            }
        }
        finally { _trayBusy = false; }
    }

    // Pinned and open: the page makes room on the right (tray width + the tray's margin + a gap).
    private void ApplyTrayDock()
        => PageRoot.Margin = new Thickness(0, 0, _trayPinned && _trayOpen ? TrayWidth + 25 : 0, 0);

    private void UpdatePinVisuals()
        => PinTrayButton.Text = _trayPinned ? "📌 Pinned" : "📌 Pin";
}