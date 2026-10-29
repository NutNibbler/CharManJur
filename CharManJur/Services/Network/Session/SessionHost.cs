using CharManJur.Models;

namespace CharManJur.Services;

public enum PlayerLinkState { Connected, Disconnected }

public enum PlayerLeaveReason { Left, Kicked, SessionClosed }

// A player as the host sees them. A Disconnected player stays on the roster and can resume.
public sealed class PlayerConnection
{
    public Guid PlayerId { get; internal set; }
    public string PlayerName { get; internal set; } = string.Empty;
    public Guid CharacterGuid { get; internal set; }
    public string CharacterName { get; internal set; } = string.Empty;
    public PlayerLinkState State { get; internal set; }
    public DateTime LastSeenUtc { get; internal set; } = DateTime.UtcNow;
    public bool IsConnected => State == PlayerLinkState.Connected;


    // What the player said about their saved character when they joined (see HelloMessage). Null when not sent.
    public string? DataHash { get; internal set; }
    public IReadOnlyDictionary<string, string>? SectionHashes { get; internal set; }
    internal NetChannel? Channel { get; set; }
}

public sealed record JoinRejection(JoinRejectReason Reason, string Message);

public sealed class SessionHostOptions
{
    public string SessionName { get; set; } = "Session";
    public string AppVersion { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 8;
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public SessionCredentials? Credentials { get; set; }          // null = no password (still encrypted)
    public AuthLimiter? AuthLimiter { get; set; }                 // null = default limiter
    public CipherKind[]? Ciphers { get; set; }
    public Func<IConnection, IConnection>? ConnectionDecorator { get; set; }   // applied after encryption
}

// The GM side. Accepts connections, runs the handshake, tracks who is connected, and raises events.
// Events are raised on pool threads: marshal to the UI thread before touching bound state.
public sealed class SessionHost : IAsyncDisposable
{
    private readonly SessionHostOptions _options;
    private readonly Guid _hostId = Guid.NewGuid();
    private readonly object _gate = new();
    private readonly List<PlayerConnection> _players = new();
    private readonly AuthLimiter _limiter;
    private readonly HashSet<Guid> _kicked = new();   // removed by the GM: cannot rejoin until allowed back

    private IListener? _listener;
    private readonly List<(IListener Listener, Task Accepting)> _extraListeners = new();   // e.g. real network sockets next to the in-app one
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _heartbeatTask;
    private bool _isOpen;   // closed by default: nobody NEW can join until the GM opens the session

    public SessionHost(SessionHostOptions? options = null)
    {
        _options = options ?? new SessionHostOptions();
        _limiter = _options.AuthLimiter ?? new AuthLimiter();
    }

    // (player, resumed): resumed = a known player reconnecting
    public event Action<PlayerConnection, bool>? PlayerJoined;
    public event Action<PlayerConnection>? PlayerStateChanged;
    public event Action<PlayerConnection, PlayerLeaveReason>? PlayerLeft;
    public event Action<PlayerConnection, Envelope>? MessageReceived;

    // Extra join checks (password, asset policy...). Runs after the built-in checks; return a rejection to refuse.
    public Func<HelloMessage, Task<JoinRejection?>>? JoinValidator { get; set; }

    public SessionHostOptions Options => _options;
    public bool IsRunning => _listener != null;
    public string? LocalDescription => _listener?.LocalDescription;

    public bool IsOpen
    {
        get { lock (_gate) return _isOpen; }
    }

    public IReadOnlyList<PlayerConnection> Players
    {
        get { lock (_gate) return _players.ToList(); }
    }

    public async Task StartAsync(ITransport transport, string address)
    {
        if (_listener != null) throw new InvalidOperationException("The host is already running.");

        _cts = new CancellationTokenSource();
        _listener = await transport.ListenAsync(address).ConfigureAwait(false);
        var listener = _listener;
        var token = _cts.Token;

        _acceptTask = Task.Run(() => AcceptLoopAsync(listener, token));
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(token));
        NetLog.Info($"host listening on {listener.LocalDescription}");
    }

    // Also accepts connections on another transport (the GM's real network port next to the in-app one).
    // Every connection from every listener goes through the same encrypted handshake and the same rules.
    public async Task<IListener> AddListenerAsync(ITransport transport, string address)
    {
        if (_listener == null || _cts == null) throw new InvalidOperationException("The host is not running.");

        var listener = await transport.ListenAsync(address).ConfigureAwait(false);
        var token = _cts.Token;
        var accepting = Task.Run(() => AcceptLoopAsync(listener, token));
        lock (_gate) _extraListeners.Add((listener, accepting));
        NetLog.Info($"host also listening on {listener.LocalDescription}");
        return listener;
    }

    // Stops one extra listener. Players already connected through it stay connected.
    public async Task RemoveListenerAsync(IListener listener)
    {
        Task? accepting = null;
        lock (_gate)
        {
            int index = _extraListeners.FindIndex(e => ReferenceEquals(e.Listener, listener));
            if (index >= 0)
            {
                accepting = _extraListeners[index].Accepting;
                _extraListeners.RemoveAt(index);
            }
        }

        listener.Stop();
        try { if (accepting != null) await accepting.ConfigureAwait(false); } catch { }
        NetLog.Info("host stopped listening on " + listener.LocalDescription);
    }

    public void SetOpen(bool open)
    {
        lock (_gate)
        {
            if (_isOpen == open) return;
            _isOpen = open;
        }
        _ = BroadcastRosterAsync();
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        var listener = _listener;
        if (listener == null) return;

        _listener = null;
        cts?.Cancel();
        listener.Stop();


        List<(IListener Listener, Task Accepting)> extras;
        lock (_gate)
        {
            extras = _extraListeners.ToList();
            _extraListeners.Clear();
        }
        foreach (var extra in extras) extra.Listener.Stop();

        List<PlayerConnection> everyone;
        lock (_gate)
        {
            _isOpen = false;
            everyone = _players.ToList();
            _players.Clear();
        }

        foreach (var player in everyone)
        {
            var channel = player.Channel;
            player.Channel = null;
            player.State = PlayerLinkState.Disconnected;
            if (channel != null)
            {
                await channel.TrySendAsync(NetMessageTypes.SessionClosing, new NoticeMessage { Message = "The GM closed the session." }).ConfigureAwait(false);
                await channel.CloseAsync().ConfigureAwait(false);
            }
            Raise(() => PlayerLeft?.Invoke(player, PlayerLeaveReason.SessionClosed));
        }

        try { if (_acceptTask != null) await _acceptTask.ConfigureAwait(false); } catch { }
        foreach (var extra in extras) { try { await extra.Accepting.ConfigureAwait(false); } catch { } }
        try { if (_heartbeatTask != null) await _heartbeatTask.ConfigureAwait(false); } catch { }
        NetLog.Info("host stopped");
    }

    public async Task<bool> KickAsync(Guid playerId, string message = "You were removed from the session by the GM.")
    {
        PlayerConnection? player;
        lock (_gate) player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return false;

        lock (_gate) _kicked.Add(playerId);
        await RemovePlayerAsync(player, PlayerLeaveReason.Kicked, NetMessageTypes.KickNotice, message).ConfigureAwait(false);
        return true;
    }

    // Lets a kicked player join again
    public void AllowRejoin(Guid playerId)
    {
        lock (_gate) _kicked.Remove(playerId);
    }

    public async Task<bool> SendToAsync<T>(Guid playerId, string type, T payload)
    {
        var channel = ChannelFor(playerId);
        return channel != null && await channel.TrySendAsync(type, payload).ConfigureAwait(false);
    }

    // Answer a request a player sent (matched by the request's id)
    public Task ReplyAsync<T>(Guid playerId, Envelope request, string type, T payload)
    {
        var channel = ChannelFor(playerId) ?? throw new InvalidOperationException("That player is not connected.");
        return channel.ReplyAsync(request, type, payload);
    }

    public async Task BroadcastAsync<T>(string type, T payload)
    {
        foreach (var channel in ConnectedChannels())
            await channel.TrySendAsync(type, payload).ConfigureAwait(false);
    }

    // Throws InvalidOperationException if that player is not connected; TimeoutException / IOException as RequestAsync.
    public Task<Envelope> RequestAsync<T>(Guid playerId, string type, T payload, TimeSpan timeout)
    {
        var channel = ChannelFor(playerId) ?? throw new InvalidOperationException("That player is not connected.");
        return channel.RequestAsync(type, payload, timeout);
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    // ---------------------------------------------------------------- accepting

    private async Task AcceptLoopAsync(IListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            IConnection? connection;
            try { connection = await listener.AcceptAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                NetLog.Error("accept loop failed", ex);
                break;
            }

            if (connection == null) break;
            _ = Task.Run(() => HandleConnectionAsync(connection));
        }
    }

    private async Task HandleConnectionAsync(IConnection rawConnection)
    {
        HandshakeResult handshake;
        try
        {
            var handshakeOptions = new HandshakeOptions { Timeout = _options.HandshakeTimeout };
            if (_options.Ciphers != null) handshakeOptions.Ciphers = _options.Ciphers;
            handshake = await SecureHandshake.AcceptAsync(rawConnection, _options.Credentials, _limiter, handshakeOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            NetLog.Error("handshake crashed", ex);
            await rawConnection.CloseAsync().ConfigureAwait(false);
            return;
        }

        if (handshake.Connection == null)
        {
            NetLog.Info($"handshake from {rawConnection.RemoteDescription} refused: {handshake.Failure}");
            await rawConnection.CloseAsync().ConfigureAwait(false);
            return;
        }

        IConnection connection = _options.ConnectionDecorator?.Invoke(handshake.Connection) ?? handshake.Connection;
        var channel = new NetChannel(connection, _hostId);
        var helloTcs = new TaskCompletionSource<Envelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.Handler = envelope =>
        {
            if (envelope.Type == NetMessageTypes.Hello) helloTcs.TrySetResult(envelope);
            return Task.CompletedTask;
        };
        channel.Start();

        try
        {
            var finished = await Task.WhenAny(helloTcs.Task, Task.Delay(_options.HandshakeTimeout)).ConfigureAwait(false);
            if (finished != helloTcs.Task)
            {
                NetLog.Warn($"no hello from {channel.RemoteDescription}; closing");
                await channel.CloseAsync().ConfigureAwait(false);
                return;
            }

            var helloEnvelope = helloTcs.Task.Result;
            var hello = EnvelopeCodec.ReadPayload<HelloMessage>(helloEnvelope);

            var rejection = Admission(hello, out _);
            if (rejection == null && JoinValidator != null && hello != null)
                rejection = await JoinValidator(hello).ConfigureAwait(false);

            PlayerConnection? player = null;
            bool resumed = false;
            NetChannel? replaced = null;

            if (rejection == null)
            {
                lock (_gate)
                {
                    // Authoritative check, now that nothing can slip in between
                    rejection = Admission(hello, out var existing);
                    if (rejection == null)
                    {
                        if (existing != null)
                        {
                            resumed = true;
                            player = existing;
                            replaced = existing.Channel;
                            player.PlayerName = hello!.PlayerName;
                            player.CharacterName = hello.CharacterName;
                        }
                        else
                        {
                            player = new PlayerConnection { PlayerId = hello!.PlayerId, CharacterGuid = hello.CharacterGuid };
                            player.PlayerName = hello.PlayerName;
                            player.CharacterName = hello.CharacterName;
                            _players.Add(player);
                        }
                        player.Channel = channel;
                        player.DataHash = SafeHash(hello.DataHash);
                        player.SectionHashes = SafeHashes(hello.SectionHashes);
                        player.State = PlayerLinkState.Connected;
                        player.LastSeenUtc = DateTime.UtcNow;
                    }
                }
            }

            if (rejection != null || player == null)
            {
                rejection ??= new JoinRejection(JoinRejectReason.BadRequest, "Could not join.");
                NetLog.Info($"rejected {channel.RemoteDescription}: {rejection.Reason}");
                await channel.TrySendAsync(NetMessageTypes.Reject,
                    new RejectMessage { Reason = rejection.Reason, Message = rejection.Message }, helloEnvelope.Id).ConfigureAwait(false);
                await channel.CloseAsync().ConfigureAwait(false);
                return;
            }

            var accepted = player;
            channel.Handler = envelope => OnPlayerMessageAsync(accepted, channel, envelope);
            channel.Closed += closed => OnChannelClosed(accepted, closed);
            if (!channel.IsOpen) OnChannelClosed(accepted, channel);   // it dropped before we subscribed

            if (replaced != null) _ = replaced.CloseAsync();   // a stale link from the same player

            await channel.TrySendAsync(NetMessageTypes.Accept, new AcceptMessage
            {
                SessionName = _options.SessionName,
                HeartbeatSeconds = (int)Math.Max(1, _options.HeartbeatInterval.TotalSeconds),
                Resumed = resumed
            }, helloEnvelope.Id).ConfigureAwait(false);

            NetLog.Info($"{accepted.PlayerName} ({accepted.CharacterName}) {(resumed ? "resumed" : "joined")}");
            Raise(() => PlayerJoined?.Invoke(accepted, resumed));
            _ = BroadcastRosterAsync();
        }
        catch (Exception ex)
        {
            NetLog.Error("handshake failed", ex);
            await channel.CloseAsync().ConfigureAwait(false);
        }
    }

    // Built-in join rules. Called without and then with the lock held; it only reads state.
    private JoinRejection? Admission(HelloMessage? hello, out PlayerConnection? existing)
    {
        existing = null;
        if (hello == null || hello.PlayerId == Guid.Empty || hello.CharacterGuid == Guid.Empty)
            return new JoinRejection(JoinRejectReason.BadRequest, "The join request was not understood.");

        if (hello.ProtocolVersion != NetProtocol.Version)
            return new JoinRejection(JoinRejectReason.VersionMismatch,
                "Your app and the GM's app are different versions. Update the app on both devices.");

        PlayerConnection? match;
        bool open;
        int count;
        bool kicked;
        lock (_gate)
        {
            kicked = _kicked.Contains(hello.PlayerId);
            match = _players.FirstOrDefault(p => p.CharacterGuid == hello.CharacterGuid);
            open = _isOpen;
            count = _players.Count;
        }
        existing = match;

        if (kicked) return new JoinRejection(JoinRejectReason.Kicked, "The GM removed you from this session.");

        if (match != null && match.PlayerId != hello.PlayerId)
            return new JoinRejection(JoinRejectReason.DuplicateCharacter, "That character is already in the session.");

        if (match != null) return null;   // a known player may always resume, even while joining is closed

        if (!open) return new JoinRejection(JoinRejectReason.Closed, "The GM is not accepting players right now.");
        if (count >= _options.MaxPlayers) return new JoinRejection(JoinRejectReason.Full, "The session is full.");
        return null;
    }

    // The fingerprints come from the player, so they are bounded: anything oversized is dropped, not trusted.
    private static string? SafeHash(string? hash) => hash is { Length: > 0 and <= 128 } ? hash : null;

    private static IReadOnlyDictionary<string, string>? SafeHashes(Dictionary<string, string>? hashes)
    {
        if (hashes == null || hashes.Count == 0 || hashes.Count > 512) return null;
        if (hashes.Any(p => p.Key.Length > 100 || p.Value is not { Length: <= 128 })) return null;
        return new Dictionary<string, string>(hashes);
    }

    // ---------------------------------------------------------------- per-player traffic

    private async Task OnPlayerMessageAsync(PlayerConnection player, NetChannel channel, Envelope envelope)
    {
        // A Leave can be handled just after the link closed (the close is seen first), so it is not subject to the stale-link check.
        if (player.Channel != channel && envelope.Type != NetMessageTypes.Leave) return;   // stale link

        switch (envelope.Type)
        {
            case NetMessageTypes.Pong:
                break;   // LastReceivedUtc already advanced on the read loop

            case NetMessageTypes.Ping:
                await channel.TrySendAsync(NetMessageTypes.Pong, new PingMessage { Ticks = DateTime.UtcNow.Ticks }, envelope.Id).ConfigureAwait(false);
                break;

            case NetMessageTypes.Leave:
                await RemovePlayerAsync(player, PlayerLeaveReason.Left, null, null).ConfigureAwait(false);
                break;

            default:
                Raise(() => MessageReceived?.Invoke(player, envelope));
                break;
        }
    }

    private void OnChannelClosed(PlayerConnection player, NetChannel channel)
    {
        bool changed = false;
        lock (_gate)
        {
            if (ReferenceEquals(player.Channel, channel) && player.State == PlayerLinkState.Connected && _players.Contains(player))
            {
                player.State = PlayerLinkState.Disconnected;
                player.Channel = null;
                player.LastSeenUtc = channel.LastReceivedUtc;
                changed = true;
            }
        }

        if (!changed) return;
        NetLog.Info($"{player.PlayerName} disconnected");
        Raise(() => PlayerStateChanged?.Invoke(player));
        _ = BroadcastRosterAsync();
    }

    private async Task RemovePlayerAsync(PlayerConnection player, PlayerLeaveReason reason, string? noticeType, string? message)
    {
        NetChannel? channel;
        lock (_gate)
        {
            if (!_players.Remove(player)) return;
            channel = player.Channel;
            player.Channel = null;
            player.State = PlayerLinkState.Disconnected;
        }

        if (channel != null)
        {
            if (noticeType != null)
                await channel.TrySendAsync(noticeType, new NoticeMessage { Message = message ?? string.Empty }).ConfigureAwait(false);
            await channel.CloseAsync().ConfigureAwait(false);
        }

        NetLog.Info($"{player.PlayerName} removed ({reason})");
        Raise(() => PlayerLeft?.Invoke(player, reason));
        _ = BroadcastRosterAsync();
    }

    // ---------------------------------------------------------------- heartbeat and roster

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                foreach (var channel in ConnectedChannels())
                {
                    if (DateTime.UtcNow - channel.LastReceivedUtc > _options.HeartbeatTimeout)
                    {
                        NetLog.Warn($"{channel.RemoteDescription} went silent; dropping the link");
                        await channel.CloseAsync().ConfigureAwait(false);
                        continue;
                    }
                    await channel.TrySendAsync(NetMessageTypes.Ping, new PingMessage { Ticks = DateTime.UtcNow.Ticks }).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private RosterMessage BuildRoster()
    {
        lock (_gate)
        {
            return new RosterMessage
            {
                SessionName = _options.SessionName,
                IsOpen = _isOpen,
                Players = _players.Select(p => new RosterEntry
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.PlayerName,
                    CharacterGuid = p.CharacterGuid,
                    CharacterName = p.CharacterName,
                    Connected = p.State == PlayerLinkState.Connected
                }).ToList()
            };
        }
    }

    private async Task BroadcastRosterAsync()
    {
        try
        {
            var roster = BuildRoster();
            foreach (var channel in ConnectedChannels())
                await channel.TrySendAsync(NetMessageTypes.Roster, roster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            NetLog.Error("roster broadcast failed", ex);
        }
    }

    private List<NetChannel> ConnectedChannels()
    {
        lock (_gate)
            return _players.Where(p => p.State == PlayerLinkState.Connected && p.Channel != null).Select(p => p.Channel!).ToList();
    }

    private NetChannel? ChannelFor(Guid playerId)
    {
        lock (_gate)
        {
            var p = _players.FirstOrDefault(x => x.PlayerId == playerId && x.State == PlayerLinkState.Connected);
            return p?.Channel;
        }
    }

    private static void Raise(Action action)
    {
        try { action(); }
        catch (Exception ex) { NetLog.Error("event handler failed", ex); }
    }
}