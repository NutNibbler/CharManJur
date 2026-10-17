using CharManJur.Models;

namespace CharManJur.Services;

// Logs changes to an item's remaining uses. Ticking uses down is consuming the item; ticking up is refilling it.
// Rapid ticks on one item are merged into a single entry, written after a short pause, so tapping +/- never
// floods the log. The net change is what gets logged, and a net change of zero logs nothing.
public sealed class UsesChangeLogger
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    private sealed class Pending
    {
        public string Name = string.Empty;
        public int TemplateId;
        public int Max;
        public int Before;
        public int After;
        public int? StackLeft;
        public CancellationTokenSource? Cts;
    }

    private readonly ICharAttribDataService _svc;
    private readonly IPlayerActionLogService _log;
    private readonly Dictionary<int, Pending> _pending = new();
    private readonly object _gate = new();

    public UsesChangeLogger(ICharAttribDataService svc, IPlayerActionLogService log)
    {
        _svc = svc;
        _log = log;
    }

    // stackLeft: set when a stack lost one item because its uses ran out; that is written immediately.
    public void Record(CharacterItem item, int before, int after, int? stackLeft = null)
    {
        if (item.Template == null || (before == after && stackLeft == null)) return;

        lock (_gate)
        {
            if (!_pending.TryGetValue(item.Id, out var p))
            {
                p = new Pending
                {
                    Name = item.DisplayName,
                    TemplateId = item.TemplateId,
                    Max = item.Template.Uses ?? 0,
                    Before = before
                };
                _pending[item.Id] = p;
            }

            p.After = after;
            if (stackLeft.HasValue) p.StackLeft = stackLeft;
            p.Cts?.Cancel();
            p.Cts = null;

            if (stackLeft.HasValue)
            {
                _ = FlushAsync(item.Id);
                return;
            }

            var cts = new CancellationTokenSource();
            p.Cts = cts;
            int key = item.Id;
            _ = Task.Delay(Delay, cts.Token).ContinueWith(
                _ => FlushAsync(key), cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }

    public async Task FlushAsync(int itemId)
    {
        Pending? p;
        lock (_gate)
        {
            if (!_pending.Remove(itemId, out p)) return;
            p.Cts?.Cancel();
        }

        if (p.Before == p.After && p.StackLeft == null) return;   // net zero: nothing happened

        string uses = $"uses {p.Before}/{p.Max} → {p.After}/{p.Max}";
        PlayerActionType type;
        string summary;

        if (p.StackLeft.HasValue)
        {
            type = PlayerActionType.ItemConsumed;
            summary = $"Used up one {p.Name} ({uses}); {p.StackLeft} left";
        }
        else if (p.After < p.Before)
        {
            type = PlayerActionType.ItemConsumed;
            summary = $"Used {p.Name}: {uses} (−{p.Before - p.After})";
        }
        else
        {
            type = PlayerActionType.ItemRefilled;
            summary = $"Refilled {p.Name}: {uses} (+{p.After - p.Before})";
        }

        await _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            ItemName = p.Name,
            ItemId = p.TemplateId,
            Summary = summary
        });
    }

    public async Task FlushAllAsync()
    {
        List<int> keys;
        lock (_gate) keys = _pending.Keys.ToList();
        foreach (int key in keys) await FlushAsync(key);
    }
}