namespace CharManJur.Services;

// Slows down password guessing: too many wrong passwords from one address in a short time locks that address out for a while.
public sealed class AuthLimiter
{
    private readonly int _maxFailures;
    private readonly TimeSpan _window;
    private readonly TimeSpan _lockout;
    private readonly Func<DateTime> _clock;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new();

    private sealed class Entry
    {
        public List<DateTime> Failures { get; } = new();
        public DateTime BlockedUntil { get; set; }
    }

    public AuthLimiter(int maxFailures = 5, TimeSpan? window = null, TimeSpan? lockout = null, Func<DateTime>? clock = null)
    {
        _maxFailures = Math.Max(1, maxFailures);
        _window = window ?? TimeSpan.FromSeconds(60);
        _lockout = lockout ?? TimeSpan.FromSeconds(60);
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    // "192.168.1.5:50321" and "[::1]:50321" become the address alone; anything else is used as it is.
    public static string KeyFor(string remoteDescription)
    {
        int colon = remoteDescription.LastIndexOf(':');
        if (colon > 0 && colon < remoteDescription.Length - 1 && remoteDescription[(colon + 1)..].All(char.IsDigit))
            return remoteDescription[..colon];
        return remoteDescription;
    }

    public bool IsBlocked(string key)
    {
        lock (_gate)
        {
            return _entries.TryGetValue(key, out var entry) && entry.BlockedUntil > _clock();
        }
    }

    public void RecordFailure(string key)
    {
        lock (_gate)
        {
            var now = _clock();
            Prune(now);

            if (!_entries.TryGetValue(key, out var entry)) _entries[key] = entry = new Entry();
            entry.Failures.Add(now);

            if (entry.Failures.Count >= _maxFailures)
            {
                entry.BlockedUntil = now + _lockout;
                entry.Failures.Clear();
                NetLog.Warn($"too many wrong passwords from {key}; locked out for {_lockout.TotalSeconds:0} s");
            }
        }
    }

    public void RecordSuccess(string key)
    {
        lock (_gate) _entries.Remove(key);
    }

    private void Prune(DateTime now)
    {
        foreach (var key in _entries.Keys.ToList())
        {
            var entry = _entries[key];
            entry.Failures.RemoveAll(t => now - t > _window);
            if (entry.Failures.Count == 0 && entry.BlockedUntil <= now) _entries.Remove(key);
        }
    }
}