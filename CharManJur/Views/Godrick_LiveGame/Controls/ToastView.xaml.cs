using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame.Controls;

public partial class ToastView : ContentView, IToastSurface
{
    private Page? _page;
    private bool _loaded;
    private int _token;

    public ToastView()
    {
        InitializeComponent();
        InputTransparent = true;
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        _loaded = true;
        _page = FindPage();
        if (_page != null) _page.Appearing += OnPageAppearing;
        GameNotices.Register(this);
    }

    private void OnViewUnloaded(object? sender, EventArgs e)
    {
        _loaded = false;
        _token++;                       // ends a notice that is mid-show
        Card.CancelAnimations();
        Card.IsVisible = false;
        if (_page != null) _page.Appearing -= OnPageAppearing;
        GameNotices.Unregister(this);
    }

    // A page came back (a modal closed): show anything that was waiting, once the transition has settled.
    private void OnPageAppearing(object? sender, EventArgs e)
        => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), GameNotices.Pump);

    private Page? FindPage()
    {
        Element? e = this;
        while (e != null && e is not Page) e = e.Parent;
        return e as Page;
    }

    // Only the page the player is actually looking at shows the notice.
    public bool CanShowNow
    {
        get
        {
            var page = _page ??= FindPage();
            if (!_loaded || page == null) return false;

            var modals = Application.Current?.MainPage?.Navigation?.ModalStack;
            if (modals is { Count: > 0 }) return ReferenceEquals(modals[modals.Count - 1], page);

            return Shell.Current?.CurrentPage is { } current ? ReferenceEquals(current, page) : true;
        }
    }

    public async Task ShowAsync(GameNotice notice, double seconds)
    {
        int token = ++_token;

        bool fromGm = notice.Source == NoticeSource.Gm;
        SourceLabel.IsVisible = fromGm;
        TitleLabel.Text = notice.Title;
        MessageLabel.Text = notice.Message;
        Card.SetDynamicResource(Border.StrokeProperty, fromGm ? "ThemeWarningButtonColor" : "ThemeDetailBorderColor1");

        Card.CancelAnimations();
        Card.TranslationY = 80;
        Card.Opacity = 0;
        Card.IsVisible = true;

        await Task.WhenAll(Card.TranslateToAsync(0, 0, 250, Easing.CubicOut), Card.FadeToAsync(1, 250));
        if (token != _token) return;

        await Task.Delay(TimeSpan.FromSeconds(seconds));
        if (token != _token) return;

        await Task.WhenAll(Card.TranslateToAsync(0, 80, 300, Easing.CubicIn), Card.FadeToAsync(0, 300));
        if (token == _token) Card.IsVisible = false;
    }
}