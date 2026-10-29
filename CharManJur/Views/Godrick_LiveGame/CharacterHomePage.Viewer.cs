namespace CharManJur.Views.Godrick_LiveGame;

// The GM's Player Viewer is this same page, opened over the dashboard on a read-only copy of a character.
// In that mode the page slides up from the bottom, and Close Viewer slides it away again.
//
// The page's own background is transparent while it loads, slides in and slides out, so the dashboard stays
// visible behind it. Without that, a blank screen shows while the heavy page is built and torn down.
public partial class CharacterHomePage
{
    private bool _viewerSlidIn;
    private bool _viewerClosing;

    private static double WindowHeight
    {
        get
        {
            var display = DeviceDisplay.Current.MainDisplayInfo;
            return display.Height / Math.Max(1, display.Density);
        }
    }

    // Called once from the constructor.
    private void InitializeViewerMode()
    {
        if (!_viewModel.IsViewer) return;

        BackgroundColor = Colors.Transparent;   // the dashboard shows through until the slide-in is done
        PageHost.TranslationY = WindowHeight;   // starts below the bottom of the window
    }

    private async Task SlideViewerInAsync()
    {
        if (!_viewModel.IsViewer || _viewerSlidIn) return;
        _viewerSlidIn = true;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(120);   // lets the page finish its first layout, so the slide isn't fighting it
        await PageHost.TranslateToAsync(0, 0, 380, Easing.CubicOut);
        System.Diagnostics.Debug.WriteLine($"[VIEWER] slide-in took {watch.ElapsedMilliseconds} ms (including the 120 ms settle)");

        ClearValue(BackgroundColorProperty);   // back to the normal page background
        UpdateIncomingFlash();                 // the screen is visible now: make sure anything waiting is flashing
    }

    // The Close Viewer button: slide away, let go of everything the page was listening to, and leave.
    public async Task CloseViewerAsync()
    {
        if (!_viewModel.IsViewer || _viewerClosing) return;
        _viewerClosing = true;

        BackgroundColor = Colors.Transparent;   // the dashboard shows through while the screen falls away
        await PageHost.TranslateToAsync(0, WindowHeight, 300, Easing.CubicIn);
        PageHost.IsVisible = false;             // nothing left to draw while the page is torn down

        _eventTrayVm?.Detach();
        _viewModel.Detach();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        await Navigation.PopModalAsync(false);
        System.Diagnostics.Debug.WriteLine($"[VIEWER] page removed in {watch.ElapsedMilliseconds} ms");
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsViewer)
        {
            _ = CloseViewerAsync();
            return true;
        }
        return base.OnBackButtonPressed();
    }
}

// Hands out the page's services, except the action log, which comes from the page's own character:
// the shared log for a normal page, a log that records nothing for the GM's viewer.
internal sealed class PageServices : IServiceProvider
{
    private readonly IServiceProvider _inner;
    private readonly CharManJur.Services.IPlayerActionLogService _log;

    public PageServices(IServiceProvider inner, CharManJur.Services.IPlayerActionLogService log)
    {
        _inner = inner;
        _log = log;
    }

    public object? GetService(Type serviceType)
        => serviceType == typeof(CharManJur.Services.IPlayerActionLogService) ? _log : _inner.GetService(serviceType);
}