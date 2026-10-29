using System.Net;
using System.Net.Sockets;

namespace CharManJur.Services;

public sealed class DiscoveryOptions
{
    public const int DefaultPort = 47201;

    public int Port { get; set; } = DefaultPort;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

    // Where announcements go. null = every connected network's broadcast address.
    public IReadOnlyList<IPAddress>? Targets { get; set; }

    // How long a session stays in the list after its last announcement (finder side)
    public TimeSpan Expiry { get; set; } = TimeSpan.FromSeconds(7);

    public int MaxSessions { get; set; } = 64;
}

// GM side: shouts the session's details on the local network every couple of seconds while it is running.
public sealed class SessionAnnouncer : IAsyncDisposable
{
    private readonly Func<SessionAnnouncement?> _snapshot;
    private readonly DiscoveryOptions _options;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private UdpClient? _udp;

    // snapshot returns the current details (called every interval so player counts stay fresh), or null to stay quiet.
    public SessionAnnouncer(Func<SessionAnnouncement?> snapshot, DiscoveryOptions? options = null)
    {
        _snapshot = snapshot;
        _options = options ?? new DiscoveryOptions();
    }

    public bool IsRunning => _loop != null;

    public void Start()
    {
        if (_loop != null) return;

        _udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _loop = Task.Run(() => RunAsync(token));
        NetLog.Info("announcing the session on the local network");
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        var loop = _loop;
        _cts = null;
        _loop = null;
        if (cts == null) return;

        cts.Cancel();
        try { if (loop != null) await loop.ConfigureAwait(false); } catch { }
        _udp?.Dispose();
        _udp = null;
        cts.Dispose();
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_options.Interval);
        try
        {
            do
            {
                await SendOnceAsync().ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { }
    }

    private async Task SendOnceAsync()
    {
        try
        {
            var announcement = _snapshot();
            var udp = _udp;
            if (announcement == null || udp == null) return;

            var bytes = announcement.Encode();
            var targets = _options.Targets ?? LocalAddresses.BroadcastTargets();
            foreach (var target in targets)
            {
                try { await udp.SendAsync(bytes, bytes.Length, new IPEndPoint(target, _options.Port)).ConfigureAwait(false); }
                catch (SocketException) { /* one adapter may be unplugged; the others still work */ }
            }
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            NetLog.Warn("announcement failed: " + ex.Message);
        }
    }
}