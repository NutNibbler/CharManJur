using CharManJur.Models;

namespace CharManJur.Services;

// Favour is an expendable currency with no maximum. Ticks are merged into one log entry after a short pause;
// a spend with a reason (a feature using it) is logged straight away.
public sealed class FavourTracker
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    private readonly ICharAttribDataService _svc;
    private readonly IPlayerActionLogService _log;
    private readonly object _gate = new();
    private int? _pendingBefore;
    private CancellationTokenSource? _cts;

    // Raised whenever Favour changes (on the calling thread).
    public event Action? Changed;

    public FavourTracker(ICharAttribDataService svc, IPlayerActionLogService log)
    {
        _svc = svc;
        _log = log;
    }

    public int Value => _svc.Favour;

    public void Adjust(int delta, string? reason = null)
    {
        int before = _svc.Favour;
        int after = Math.Max(0, before + delta);
        if (after == before) return;

        _svc.Favour = after;
        Changed?.Invoke();

        if (reason != null)
        {
            _ = LogAsync($"{reason}: Favour {before} → {after}");
            _ = _svc.SaveNowAsync();
            return;
        }

        lock (_gate)
        {
            _pendingBefore ??= before;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Delay(Delay, token).ContinueWith(
                _ => FlushAsync(), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }

    // Writes the merged entry now (when the Event Tray opens and when the session ends).
    public async Task FlushAsync()
    {
        int? before;
        lock (_gate)
        {
            before = _pendingBefore;
            _pendingBefore = null;
            _cts?.Cancel();
            _cts = null;
        }
        if (before == null) return;

        int after = _svc.Favour;
        if (after == before.Value) return;   // net zero: nothing happened

        await LogAsync(after > before.Value
            ? $"Gained Favour: {before} → {after} (+{after - before.Value})"
            : $"Spent Favour: {before} → {after} ({after - before.Value})");
        await _svc.SaveNowAsync();
    }

    private Task LogAsync(string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.FavourChanged,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });
}