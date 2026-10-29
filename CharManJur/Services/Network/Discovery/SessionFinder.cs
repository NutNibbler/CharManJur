using System.Net;
using System.Net.Sockets;

namespace CharManJur.Services;

public sealed record FoundSession(SessionAnnouncement Announcement, IPAddress Host, DateTime LastSeenUtc)
{
    public string Address => $"{Host}:{Announcement.TcpPort}";   // what TcpTransport connects to
    public string InviteCode => CharManJur.Services.InviteCode.Encode(Host, Announcement.TcpPort, Announcement.PasswordRequired);
}

// Player side: listens for announcements and keeps a list of the sessions it can currently hear.
// Events are raised on pool threads: marshal to the UI thread before touching bound state.
public sealed class SessionFinder : IAsyncDisposable
{
    private readonly DiscoveryOptions _options;
    private readonly Func<DateTime> _clock;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, FoundSession> _found = new();
    private CancellationTokenSource? _cts;
    private UdpClient? _udp;
    private Task? _receiveLoop;
    private Task? _expiryLoop;

    public SessionFinder(DiscoveryOptions? options = null, Func<DateTime>? clock = null)
    {
        _options = options ?? new DiscoveryOptions();
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public event Action? Changed;

    public bool IsRunning => _receiveLoop != null;

    public IReadOnlyList<FoundSession> Sessions
    {
        get
        {
            lock (_gate)
                return _found.Values.OrderBy(f => f.Announcement.SessionName, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    // Throws SocketException if the port cannot be opened (another app, a firewall): the UI shows "type the code instead".
    public void Start()
    {
        if (_receiveLoop != null) return;

        var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);   // two copies of the app on one PC can both listen
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, _options.Port));
        _udp = udp;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _receiveLoop = Task.Run(() => ReceiveAsync(udp, token));
        _expiryLoop = Task.Run(() => ExpireAsync(token));
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts == null) return;
        _cts = null;

        cts.Cancel();
        _udp?.Dispose();   // unblocks the receive
        try { if (_receiveLoop != null) await _receiveLoop.ConfigureAwait(false); } catch { }
        try { if (_expiryLoop != null) await _expiryLoop.ConfigureAwait(false); } catch { }
        _receiveLoop = null;
        _expiryLoop = null;
        _udp = null;
        cts.Dispose();

        bool hadAny;
        lock (_gate)
        {
            hadAny = _found.Count > 0;
            _found.Clear();
        }
        if (hadAny) RaiseChanged();
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task ReceiveAsync(UdpClient udp, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { continue; }   // e.g. a reset from an unreachable sender: keep listening
            catch (Exception ex)
            {
                NetLog.Warn("discovery receive failed: " + ex.Message);
                break;
            }

            try { Handle(result); }
            catch (Exception ex) { NetLog.Warn("bad announcement ignored: " + ex.Message); }
        }
    }

    private void Handle(UdpReceiveResult result)
    {
        var announcement = SessionAnnouncement.TryDecode(result.Buffer);
        if (announcement == null) return;

        bool changed;
        lock (_gate)
        {
            // A flood of made-up sessions cannot grow the list without limit
            if (!_found.ContainsKey(announcement.SessionId) && _found.Count >= _options.MaxSessions) return;

            var entry = new FoundSession(announcement, result.RemoteEndPoint.Address, _clock());
            changed = !_found.TryGetValue(announcement.SessionId, out var old)
                      || old.Announcement != announcement || !old.Host.Equals(entry.Host);
            _found[announcement.SessionId] = entry;
        }

        if (changed) RaiseChanged();
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        var step = TimeSpan.FromMilliseconds(Math.Max(50, _options.Expiry.TotalMilliseconds / 4));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(step, ct).ConfigureAwait(false);

                bool removed = false;
                lock (_gate)
                {
                    var now = _clock();
                    foreach (var id in _found.Where(kv => now - kv.Value.LastSeenUtc > _options.Expiry).Select(kv => kv.Key).ToList())
                        removed |= _found.Remove(id);
                }
                if (removed) RaiseChanged();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void RaiseChanged()
    {
        try { Changed?.Invoke(); }
        catch (Exception ex) { NetLog.Error("discovery handler failed", ex); }
    }
}