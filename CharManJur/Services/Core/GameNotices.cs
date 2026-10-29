namespace CharManJur.Services;

public enum NoticeSource { Player, Gm }

// One short notice for the toast. A GM-sent notice (multiplayer, not built yet) is posted with NoticeSource.Gm.
public sealed record GameNotice(string Title, string Message, NoticeSource Source = NoticeSource.Player, double Seconds = 5);

// A page that can show toasts (the ToastView control).
public interface IToastSurface
{
    bool CanShowNow { get; }
    Task ShowAsync(GameNotice notice, double seconds);
}

// Posts notices from anywhere. They show one after another on whichever page is on top; if no page can show
// them right now (a page without a toast is open), they wait until one appears.
public static class GameNotices
{
    private static readonly List<IToastSurface> Surfaces = new();
    private static readonly Queue<GameNotice> Pending = new();
    private static bool _pumping;
    private const int MaxPending = 8;

    public static void Post(GameNotice notice)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            while (Pending.Count >= MaxPending) Pending.Dequeue();
            Pending.Enqueue(notice);
            _ = PumpAsync();
        });
    }

    public static void Register(IToastSurface surface)
    {
        if (!Surfaces.Contains(surface)) Surfaces.Add(surface);
        _ = PumpAsync();
    }

    public static void Unregister(IToastSurface surface) => Surfaces.Remove(surface);

    // Tries again to show waiting notices (a page appeared, a modal closed...).
    public static void Pump() => _ = PumpAsync();

    private static async Task PumpAsync()
    {
        if (_pumping) return;
        _pumping = true;
        try
        {
            while (Pending.Count > 0)
            {
                var surface = Surfaces.FirstOrDefault(s => s.CanShowNow);
                if (surface == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[TOAST] {Pending.Count} notice(s) waiting; {Surfaces.Count} surface(s) registered, none can show right now");
                    return;
                }

                var notice = Pending.Dequeue();
                // Others are waiting: keep this one shorter so they don't back up.
                double seconds = Pending.Count > 0 ? Math.Min(notice.Seconds, 3) : notice.Seconds;
                await surface.ShowAsync(notice, seconds);
            }
        }
        finally { _pumping = false; }
    }
}