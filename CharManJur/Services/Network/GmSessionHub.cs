using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CharManJur.Models;

namespace CharManJur.Services;

// A player running inside this same app (a test player or a Network Lab player), as the hub sees it.
public sealed record LocalPlayerInfo(Guid PlayerId, string PlayerName, string CharacterName, ClientState State);

// Where players on the local network can reach this session.
public sealed record NetworkHostingInfo(
    int Port,
    IReadOnlyList<string> Addresses,   // "192.168.1.25:47200", best first
    string InviteCode,                 // "7QK4-M2XD-9HTA": the first address, as a short code
    bool PasswordRequired,
    bool Announcing);                  // shouting on the network so players find it without typing anything

// The outcome of asking one network player for a fresh copy of their character.
public sealed record SnapshotResult(PlayerConnection Player, bool Received, string Message);

// The one place the GM side of the app talks to the network core. It owns the session host, the in-memory
// network the test players use, and the live fault settings. Everything here is raised on pool threads:
// marshal to the UI thread before touching anything bound to the screen.
public interface IGmSessionHub
{
    bool IsRunning { get; }
    bool IsOpen { get; }
    Guid CampaignId { get; }          // Guid.Empty for a Network Lab session
    bool IsLabSession { get; }
    string Address { get; }
    string SessionName { get; }
    ChaosOptions Chaos { get; }
    IReadOnlyList<PlayerConnection> HostPlayers { get; }
    IReadOnlyList<LocalPlayerInfo> LocalPlayers { get; }
    IReadOnlyList<string> TraceLines { get; }
    NetworkHostingInfo? Network { get; }   // null until StartNetworkAsync

    // One listener each (the dashboard replaces them whenever it loads, so a closed dashboard never keeps listening).
    Action<PlayerConnection, bool>? OnPlayerJoined { get; set; }          // (player, resumed)
    Action<PlayerConnection>? OnPlayerStateChanged { get; set; }          // connected / disconnected
    Action<PlayerConnection, PlayerLeaveReason>? OnPlayerLeft { get; set; }

    // A player's copy of their character arrived (when they leave, or when asked). Return true once it is stored: the player is told.
    // Runs on a pool thread.
    Func<PlayerConnection, CharacterSnapshotMessage, Task<bool>>? OnCharacterSnapshot { get; set; }

    // A network player's live stat chips changed. Runs on a pool thread; SummaryFor gives the latest at any time.
    Action<PlayerConnection, CharacterSummaryMessage>? OnCharacterSummary { get; set; }
    CharacterSummaryMessage? SummaryFor(Guid playerId);

    // Anything about the session changed (the Network Lab refreshes on this) / a new trace line.
    event Action? Changed;
    event Action<string>? TraceAdded;

    // credentials: null = no password (the link is still encrypted)
    Task StartAsync(Guid campaignId, string sessionName, SessionCredentials? credentials = null);
    void SetOpen(bool open);
    Task StopAsync();

    // Lets players on the real network join (next to the test players inside the app) and announces the session.
    // Tries the preferred port first and falls back to any free one. The session must be running.
    Task<NetworkHostingInfo> StartNetworkAsync(int preferredPort = NetProtocol.DefaultPort);
    Task StopNetworkAsync();

    // A player that lives inside this app and joins over the in-memory network.
    Task<JoinResult> JoinLocalPlayerAsync(HelloMessage hello, string? password = null);
    Task LeaveLocalPlayerAsync(Guid playerId);

    Task<bool> KickAsync(Guid playerId);

    // Asks every player on the network (not the test players inside this app) for a fresh copy of their character and
    // waits up to the timeout for the answers. Each copy goes through OnCharacterSnapshot. One result per player.
    Task<IReadOnlyList<SnapshotResult>> RequestSnapshotsAsync(TimeSpan timeout);

    // Sends one GM action to a network player and waits for their app to confirm it. Sending the same action again
    // (same ActionId) is safe: the player's app recognises it.
    Task<GmActionResult> SendActionAsync(Guid playerId, GmActionMessage action, TimeSpan timeout);

    // Asks one network player for a fresh copy of their character (they save first) WITHOUT storing it in the campaign.
    // The GM uses it as the starting point for a bestow. Timeout and refusals come back as Received = false.
    Task<CopyResult> RequestCopyAsync(Guid playerId, string reason, TimeSpan timeout);

    // Cuts every link at once (like the Wi-Fi dropping); the players reconnect on their own.
    Task DropAllLinksAsync();
}

public sealed record GmActionResult(bool Delivered, string Message);

public sealed record CopyResult(bool Received, string Message, CharacterSnapshotMessage? Snapshot);

public sealed class GmSessionHub : IGmSessionHub
{
    private const int TraceLimit = 300;

    private readonly TcpTransport _tcp = new();
    private readonly DiscoveryOptions _discovery;
    private readonly string _bindHost;
    private readonly LoopbackTransport _loopback = new();
    private readonly ChaosTransport _network;   // only adds faults to links, after they are encrypted
    private readonly object _gate = new();
    private readonly Dictionary<Guid, LocalPlayer> _locals = new();
    private readonly Queue<string> _trace = new();
    private readonly Dictionary<Guid, CharacterSummaryMessage> _summaries = new();
    private SessionHost? _host;
    private Guid _campaignId;
    private string _address = string.Empty;
    private string _sessionName = string.Empty;
    private bool _passwordRequired;
    private byte[]? _passwordKey;   // lets this app's own test players in without typing the password
    private Guid _announceId;
    private IListener? _tcpListener;
    private SessionAnnouncer? _announcer;
    private NetworkHostingInfo? _hosting;

    private sealed record LocalPlayer(SessionClient Client, HelloMessage Hello);

    public GmSessionHub() : this(new DiscoveryOptions(), string.Empty)
    {
    }

    // For tests: a private discovery port/target, and an address to bind (127.0.0.1) so no firewall prompt appears.
    internal GmSessionHub(DiscoveryOptions discovery, string bindHost)
    {
        _discovery = discovery;
        _bindHost = bindHost;
        _network = new ChaosTransport(_loopback);
        NetLog.Sink = AddTrace;
    }

    public bool IsRunning => _host?.IsRunning == true;
    public bool IsOpen => _host?.IsOpen == true;
    public Guid CampaignId => _campaignId;
    public bool IsLabSession => IsRunning && _campaignId == Guid.Empty;
    public string Address => _address;
    public string SessionName => _sessionName;
    public ChaosOptions Chaos => _network.Options;
    public NetworkHostingInfo? Network => _hosting;

    public IReadOnlyList<PlayerConnection> HostPlayers => _host?.Players ?? (IReadOnlyList<PlayerConnection>)Array.Empty<PlayerConnection>();

    public IReadOnlyList<LocalPlayerInfo> LocalPlayers
    {
        get
        {
            lock (_gate)
                return _locals.Select(kv => new LocalPlayerInfo(
                    kv.Key, kv.Value.Hello.PlayerName, kv.Value.Hello.CharacterName, kv.Value.Client.State)).ToList();
        }
    }

    public IReadOnlyList<string> TraceLines
    {
        get
        {
            lock (_trace) return _trace.ToList();
        }
    }

    public Action<PlayerConnection, bool>? OnPlayerJoined { get; set; }
    public Action<PlayerConnection>? OnPlayerStateChanged { get; set; }
    public Action<PlayerConnection, PlayerLeaveReason>? OnPlayerLeft { get; set; }
    public Func<PlayerConnection, CharacterSnapshotMessage, Task<bool>>? OnCharacterSnapshot { get; set; }
    public Action<PlayerConnection, CharacterSummaryMessage>? OnCharacterSummary { get; set; }

    public event Action? Changed;
    public event Action<string>? TraceAdded;

    // ---------------------------------------------------------------- the session

    public async Task StartAsync(Guid campaignId, string sessionName, SessionCredentials? credentials = null)
    {
        await StopAsync().ConfigureAwait(false);   // never two sessions at once

        var host = new SessionHost(new SessionHostOptions
        {
            SessionName = sessionName,
            MaxPlayers = 8,
            AppVersion = string.Empty,
            Credentials = credentials,
            ConnectionDecorator = _network.Wrap
        });

        host.PlayerJoined += (player, resumed) => { Safe(() => OnPlayerJoined?.Invoke(player, resumed)); RaiseChanged(); };
        host.PlayerStateChanged += player => { Safe(() => OnPlayerStateChanged?.Invoke(player)); RaiseChanged(); };
        host.PlayerLeft += (player, reason) =>
        {
            lock (_gate) _summaries.Remove(player.PlayerId);
            Safe(() => OnPlayerLeft?.Invoke(player, reason));
            RaiseChanged();
        };
        host.MessageReceived += (player, envelope) => OnHostMessage(host, player, envelope);

        string address = "gm-" + campaignId.ToString("N");
        await host.StartAsync(_loopback, address).ConfigureAwait(false);

        _host = host;
        _passwordRequired = credentials != null;
        _passwordKey = credentials?.Key;
        _announceId = campaignId != Guid.Empty ? campaignId : Guid.NewGuid();
        _campaignId = campaignId;
        _address = address;
        _sessionName = sessionName;
        RaiseChanged();
    }

    public void SetOpen(bool open)
    {
        _host?.SetOpen(open);
        RaiseChanged();
    }

    public async Task StopAsync()
    {
        SessionHost? host;
        List<LocalPlayer> locals;
        lock (_gate)
        {
            host = _host;
            _host = null;
            locals = _locals.Values.ToList();
            _locals.Clear();
            _summaries.Clear();
        }

        _address = string.Empty;
        _sessionName = string.Empty;
        _campaignId = Guid.Empty;
        _passwordRequired = false;
        _passwordKey = null;

        await StopAnnouncerAsync().ConfigureAwait(false);
        _tcpListener = null;   // the host stops its own listeners below
        _hosting = null;

        if (host != null) await host.StopAsync().ConfigureAwait(false);   // tells the players the session closed
        foreach (var local in locals) await local.Client.DisposeAsync().ConfigureAwait(false);

        if (host != null) RaiseChanged();
    }

    public async Task<NetworkHostingInfo> StartNetworkAsync(int preferredPort = NetProtocol.DefaultPort)
    {
        var host = _host;
        if (host == null || !host.IsRunning) throw new InvalidOperationException("Open the session before opening it to the network.");
        if (_hosting != null) return _hosting;

        IListener listener;
        try
        {
            listener = await host.AddListenerAsync(_tcp, $"{_bindHost}:{preferredPort}").ConfigureAwait(false);
        }
        catch (SocketException) when (preferredPort != 0)
        {
            NetLog.Warn($"port {preferredPort} is busy; using another one");
            listener = await host.AddListenerAsync(_tcp, $"{_bindHost}:0").ConfigureAwait(false);
        }

        _tcpListener = listener;
        string local = listener.LocalDescription;
        int port = int.Parse(local[(local.LastIndexOf(':') + 1)..]);

        var addresses = LocalAddresses.LanIPv4().ToList();
        if (addresses.Count == 0) addresses.Add(IPAddress.Loopback);   // no network at all: still reachable from this PC
        var texts = addresses.Select(a => $"{a}:{port}").ToList();
        string code = InviteCode.Encode(addresses[0], port, _passwordRequired);

        _announcer = new SessionAnnouncer(() => AnnouncementNow(port), _discovery);
        try
        {
            _announcer.Start();
        }
        catch (Exception ex)
        {
            NetLog.Warn("could not announce the session (players can still type the code): " + ex.Message);
            _announcer = null;
        }

        _hosting = new NetworkHostingInfo(port, texts, code, _passwordRequired, _announcer != null);
        RaiseChanged();
        return _hosting;
    }

    public async Task StopNetworkAsync()
    {
        var listener = _tcpListener;
        _tcpListener = null;
        _hosting = null;

        await StopAnnouncerAsync().ConfigureAwait(false);
        if (listener != null && _host != null) await _host.RemoveListenerAsync(listener).ConfigureAwait(false);
        RaiseChanged();
    }

    private SessionAnnouncement? AnnouncementNow(int port)
    {
        var host = _host;
        if (host == null || !host.IsRunning) return null;

        string name = string.IsNullOrWhiteSpace(_sessionName) ? "Session" : _sessionName;
        return new SessionAnnouncement(_announceId, name, port, _passwordRequired, host.IsOpen, host.Players.Count, host.Options.MaxPlayers);
    }

    private async Task StopAnnouncerAsync()
    {
        var announcer = _announcer;
        _announcer = null;
        if (announcer != null) await announcer.StopAsync().ConfigureAwait(false);
    }

    public async Task<bool> KickAsync(Guid playerId)
    {
        var host = _host;
        return host != null && await host.KickAsync(playerId).ConfigureAwait(false);
    }

    public Task DropAllLinksAsync() => _network.DropAllConnectionsAsync();

    // ---------------------------------------------------------------- players inside this app

    public async Task<JoinResult> JoinLocalPlayerAsync(HelloMessage hello, string? password = null)
    {
        var host = _host;
        if (host == null || !host.IsRunning)
            return new JoinResult(false, null, "The session is not running.");

        var client = new SessionClient(_loopback, new SessionClientOptions
        {
            ConnectionDecorator = _network.Wrap,
            PasswordKey = string.IsNullOrEmpty(password) ? _passwordKey : null   // the GM's own app does not retype its password
        });
        client.StateChanged += state =>
        {
            if (state is ClientState.Kicked or ClientState.SessionClosed or ClientState.Left or ClientState.Rejected)
                ForgetLocal(hello.PlayerId, client);
            RaiseChanged();
        };

        var result = await client.JoinAsync(_address, hello, password).ConfigureAwait(false);
        if (!result.Accepted)
        {
            await client.DisposeAsync().ConfigureAwait(false);
            return result;
        }

        lock (_gate) _locals[hello.PlayerId] = new LocalPlayer(client, hello);
        RaiseChanged();
        return result;
    }

    public async Task LeaveLocalPlayerAsync(Guid playerId)
    {
        LocalPlayer? local;
        lock (_gate) _locals.TryGetValue(playerId, out local);
        if (local == null) return;

        await local.Client.LeaveAsync().ConfigureAwait(false);
        ForgetLocal(playerId, local.Client);
    }

    private void ForgetLocal(Guid playerId, SessionClient client)
    {
        bool removed = false;
        lock (_gate)
        {
            if (_locals.TryGetValue(playerId, out var local) && ReferenceEquals(local.Client, client))
                removed = _locals.Remove(playerId);
        }

        if (removed) _ = client.DisposeAsync().AsTask();
    }

    // ---------------------------------------------------------------- tracing and events

    private void AddTrace(string line)
    {
        Debug.WriteLine(line);
        lock (_trace)
        {
            _trace.Enqueue(line);
            while (_trace.Count > TraceLimit) _trace.Dequeue();
        }
        Safe(() => TraceAdded?.Invoke(line));
    }

    // ---------------------------------------------------------------- character copies

    private void OnHostMessage(SessionHost host, PlayerConnection player, Envelope envelope)
    {
        if (envelope.Type == NetMessageTypes.CharacterSnapshot)
            _ = HandleSnapshotAsync(host, player, envelope);
        else if (envelope.Type == NetMessageTypes.CharacterSummary)
            HandleSummary(player, envelope);
    }

    // ---------------------------------------------------------------- live stat chips

    public CharacterSummaryMessage? SummaryFor(Guid playerId)
    {
        lock (_gate) return _summaries.TryGetValue(playerId, out var summary) ? summary : null;
    }

    private void HandleSummary(PlayerConnection player, Envelope envelope)
    {
        var clean = CleanSummary(player, EnvelopeCodec.ReadPayload<CharacterSummaryMessage>(envelope));
        if (clean == null) return;

        lock (_gate) _summaries[player.PlayerId] = clean;
        Safe(() => OnCharacterSummary?.Invoke(player, clean));
    }

    // The chips come from the player, so they are bounded and only taken for the player's own character.
    private static CharacterSummaryMessage? CleanSummary(PlayerConnection player, CharacterSummaryMessage? message)
    {
        if (message == null || message.CharacterGuid != player.CharacterGuid) return null;

        return new CharacterSummaryMessage
        {
            CharacterGuid = message.CharacterGuid,
            Compact = Clip(message.Compact, 200),
            Waiting = Math.Clamp(message.Waiting, 0, 999),
            Chips = (message.Chips ?? new List<SummaryChip>()).Take(16)
                .Select(c => new SummaryChip { Label = Clip(c.Label, 24), Value = Clip(c.Value, 64), Level = Math.Clamp(c.Level, 0, 2) })
                .ToList()
        };
    }

    private static string Clip(string? text, int max) => text == null ? string.Empty : text.Length <= max ? text : text[..max];

    // A player sent their character on their own (leaving the session): store it, then tell them whether it worked.
    private async Task HandleSnapshotAsync(SessionHost host, PlayerConnection player, Envelope envelope)
    {
        var (ok, message) = await AcceptSnapshotAsync(player, EnvelopeCodec.ReadPayload<CharacterSnapshotMessage>(envelope)).ConfigureAwait(false);
        try
        {
            await host.ReplyAsync(player.PlayerId, envelope, NetMessageTypes.Ack,
                new AckMessage { Ok = ok, Message = ok ? null : message }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            NetLog.Error("could not confirm a character copy to the player", ex);
        }
    }

    // A copy is only taken from the player it belongs to.
    private async Task<(bool Ok, string Message)> AcceptSnapshotAsync(PlayerConnection player, CharacterSnapshotMessage? message)
    {
        if (message == null) return (false, "The character copy was not understood.");
        if (message.CharacterGuid != player.CharacterGuid) return (false, "That copy belongs to a different character.");

        var handler = OnCharacterSnapshot;
        if (handler == null) return (false, "The GM is not ready to receive character copies.");

        try
        {
            return await handler(player, message).ConfigureAwait(false)
                ? (true, string.Empty)
                : (false, "The GM could not store the copy.");
        }
        catch (Exception ex)
        {
            NetLog.Error("storing a character copy failed", ex);
            return (false, "The GM could not store the copy.");
        }
    }

    public async Task<IReadOnlyList<SnapshotResult>> RequestSnapshotsAsync(TimeSpan timeout)
    {
        var host = _host;
        if (host == null) return Array.Empty<SnapshotResult>();

        HashSet<Guid> insideThisApp;
        lock (_gate) insideThisApp = _locals.Keys.ToHashSet();

        var targets = host.Players.Where(p => !insideThisApp.Contains(p.PlayerId)).ToList();
        return await Task.WhenAll(targets.Select(p => RequestSnapshotAsync(host, p, timeout))).ConfigureAwait(false);
    }

    private async Task<SnapshotResult> RequestSnapshotAsync(SessionHost host, PlayerConnection player, TimeSpan timeout)
    {
        if (!player.IsConnected) return new SnapshotResult(player, false, "Not connected right now.");

        try
        {
            var reply = await host.RequestAsync(player.PlayerId, NetMessageTypes.SnapshotRequest,
                new SnapshotRequestMessage { Reason = "session closing" }, timeout).ConfigureAwait(false);

            if (reply.Type != NetMessageTypes.CharacterSnapshot)
                return new SnapshotResult(player, false, EnvelopeCodec.ReadPayload<AckMessage>(reply)?.Message ?? "The player could not send a copy.");

            var (ok, message) = await AcceptSnapshotAsync(player, EnvelopeCodec.ReadPayload<CharacterSnapshotMessage>(reply)).ConfigureAwait(false);
            return new SnapshotResult(player, ok, message);
        }
        catch (TimeoutException)
        {
            return new SnapshotResult(player, false, "No answer in time.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return new SnapshotResult(player, false, "Not connected right now.");
        }
    }

    public async Task<GmActionResult> SendActionAsync(Guid playerId, GmActionMessage action, TimeSpan timeout)
    {
        var host = _host;
        var player = host?.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (host == null || player == null) return new GmActionResult(false, "The player is not in the session.");
        if (!player.IsConnected) return new GmActionResult(false, "The player's connection is lost. Try again when they are back.");

        try
        {
            var reply = await host.RequestAsync(playerId, NetMessageTypes.GmAction, action, timeout).ConfigureAwait(false);
            var ack = EnvelopeCodec.ReadPayload<AckMessage>(reply);
            return ack?.Ok == true
                ? new GmActionResult(true, ack.Message ?? "Delivered.")
                : new GmActionResult(false, ack?.Message ?? "The player's app did not accept it.");
        }
        catch (TimeoutException)
        {
            return new GmActionResult(false, "No confirmation in time. It may still arrive. Sending it again is safe.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return new GmActionResult(false, "The connection was lost before it was confirmed.");
        }
    }

    private void RaiseChanged() => Safe(() => Changed?.Invoke());

    public async Task<CopyResult> RequestCopyAsync(Guid playerId, string reason, TimeSpan timeout)
    {
        var host = _host;
        var player = host?.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (host == null || player == null) return new CopyResult(false, "The player is not in the session.", null);
        if (!player.IsConnected) return new CopyResult(false, "The player's connection is lost. Try again when they are back.", null);

        try
        {
            var reply = await host.RequestAsync(playerId, NetMessageTypes.SnapshotRequest,
                new SnapshotRequestMessage { Reason = reason }, timeout).ConfigureAwait(false);

            if (reply.Type != NetMessageTypes.CharacterSnapshot)
                return new CopyResult(false, EnvelopeCodec.ReadPayload<AckMessage>(reply)?.Message ?? "The player could not send a copy.", null);

            var snapshot = EnvelopeCodec.ReadPayload<CharacterSnapshotMessage>(reply);
            if (snapshot == null || snapshot.CharacterGuid != player.CharacterGuid)
                return new CopyResult(false, "The player sent a copy of a different character.", null);

            return new CopyResult(true, "Received.", snapshot);
        }
        catch (TimeoutException)
        {
            return new CopyResult(false, "No answer in time.", null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return new CopyResult(false, "The connection was lost before the copy arrived.", null);
        }
    }

    private static void Safe(Action action)
    {
        try { action(); }
        catch (Exception ex) { NetLog.Error("hub event handler failed", ex); }
    }
}