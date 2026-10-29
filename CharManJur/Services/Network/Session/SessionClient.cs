using CharManJur.Models;

namespace CharManJur.Services;

public enum ClientState
{
    Idle,           // never joined
    Connecting,
    Connected,
    Reconnecting,   // link lost; retrying. To the player this IS the "GM connection lost" freeze.
    Disconnected,   // link lost and auto-reconnect is off
    Rejected,
    Kicked,
    SessionClosed,
    Left
}

public sealed record JoinResult(bool Accepted, JoinRejectReason? Reason, string Message);

public sealed class SessionClientOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HostSilenceTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public bool AutoReconnect { get; set; } = true;
    public CipherKind[]? Ciphers { get; set; }
    public Func<IConnection, IConnection>? ConnectionDecorator { get; set; }

    // Only for a player inside the host's own app: the session password's derived key, so no password has to be typed.
    public byte[]? PasswordKey { get; set; }

    // Waits between attempts; the last value repeats forever (a dropped player may rejoin with no time limit).
    public TimeSpan[] ReconnectDelays { get; set; } =
    {
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(15)
    };
}

// The player side. Joins a host, answers heartbeats, keeps the roster, and reconnects (resuming) when the link drops.
// Events are raised on pool threads: marshal to the UI thread before touching bound state.
public sealed class SessionClient : IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly SessionClientOptions _options;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _lifetime = new();

    private NetChannel? _channel;
    private ClientState _state = ClientState.Idle;
    private string _address = string.Empty;
    private string? _password;      // memory only, to rejoin after a dropped link
    private HelloMessage _hello = new();
    private bool _intentional;      // kick / session closed / leave: do not reconnect
    private bool _reconnecting;
    private bool _watchdogStarted;

    public SessionClient(ITransport transport, SessionClientOptions? options = null)
    {
        _transport = transport;
        _options = options ?? new SessionClientOptions();
    }

    public event Action<ClientState>? StateChanged;
    public event Action<RosterMessage>? RosterChanged;
    public event Action<Envelope>? MessageReceived;

    public ClientState State
    {
        get { lock (_gate) return _state; }
    }

    public RosterMessage? Roster { get; private set; }
    public string LastMessage { get; private set; } = string.Empty;   // kick / close / reject text for the UI

    public async Task<JoinResult> JoinAsync(string address, HelloMessage hello, string? password = null)
    {
        _address = address;
        _hello = hello;
        _password = password;
        _intentional = false;
        SetState(ClientState.Connecting);

        JoinResult result;
        try
        {
            result = await ConnectOnceAsync(resume: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LastMessage = $"Could not reach the host: {ex.Message}";
            SetState(ClientState.Idle);
            return new JoinResult(false, null, LastMessage);
        }

        if (result.Accepted)
        {
            SetState(ClientState.Connected);
            StartWatchdog();
        }
        else
        {
            LastMessage = result.Message;
            SetState(ClientState.Rejected);
        }
        return result;
    }

    public Task SendAsync<T>(string type, T payload) => RequireChannel().SendAsync(type, payload);

    // Answer a request the host sent (matched by the request's id)
    public Task ReplyAsync<T>(Envelope request, string type, T payload) => RequireChannel().ReplyAsync(request, type, payload);

    public Task<Envelope> RequestAsync<T>(string type, T payload, TimeSpan timeout)
        => RequireChannel().RequestAsync(type, payload, timeout);

    public async Task LeaveAsync()
    {
        NetChannel? channel;
        lock (_gate)
        {
            _intentional = true;
            channel = _channel;
            _channel = null;
        }

        if (channel != null)
        {
            await channel.TrySendAsync(NetMessageTypes.Leave, new NoticeMessage()).ConfigureAwait(false);
            await channel.CloseAsync().ConfigureAwait(false);
        }
        _lifetime.Cancel();
        SetState(ClientState.Left);
    }

    public async ValueTask DisposeAsync()
    {
        NetChannel? channel;
        lock (_gate)
        {
            _intentional = true;
            channel = _channel;
            _channel = null;
        }
        _lifetime.Cancel();
        if (channel != null) await channel.CloseAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------- connecting

    private async Task<JoinResult> ConnectOnceAsync(bool resume)
    {
        IConnection raw;
        using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token))
        {
            connectCts.CancelAfter(_options.ConnectTimeout);
            raw = await _transport.ConnectAsync(_address, connectCts.Token).ConfigureAwait(false);
        }

        var handshakeOptions = new HandshakeOptions { Timeout = _options.HandshakeTimeout };
        if (_options.Ciphers != null) handshakeOptions.Ciphers = _options.Ciphers;
        handshakeOptions.PasswordKey = _options.PasswordKey;
        var handshake = await SecureHandshake.ConnectAsync(raw, _password, handshakeOptions, _lifetime.Token).ConfigureAwait(false);
        if (handshake.Connection == null)
        {
            await raw.CloseAsync().ConfigureAwait(false);
            return new JoinResult(false, ReasonFor(handshake.Failure), handshake.Message);
        }

        IConnection connection = _options.ConnectionDecorator?.Invoke(handshake.Connection) ?? handshake.Connection;

        var channel = new NetChannel(connection, _hello.PlayerId);
        channel.Handler = envelope => OnHostMessageAsync(channel, envelope);
        channel.Closed += OnChannelClosed;
        channel.Start();

        var hello = new HelloMessage
        {
            AppVersion = _hello.AppVersion,
            ProtocolVersion = _hello.ProtocolVersion,
            PlayerId = _hello.PlayerId,
            PlayerName = _hello.PlayerName,
            CharacterGuid = _hello.CharacterGuid,
            CharacterName = _hello.CharacterName,
            Resume = resume,
            // Only the first connection says what the character looked like when it was loaded: after an automatic
            // reconnect the character has moved on, and the GM only compares the first join.
            DataHash = resume ? null : _hello.DataHash,
            SectionHashes = resume ? null : _hello.SectionHashes
        };

        Envelope reply;
        try
        {
            reply = await channel.RequestAsync(NetMessageTypes.Hello, hello, _options.HandshakeTimeout).ConfigureAwait(false);
        }
        catch
        {
            await channel.CloseAsync().ConfigureAwait(false);
            throw;
        }

        if (reply.Type == NetMessageTypes.Accept)
        {
            lock (_gate) _channel = channel;
            if (!channel.IsOpen) OnChannelClosed(channel);   // it dropped before we recorded it
            return new JoinResult(true, null, string.Empty);
        }

        var reject = EnvelopeCodec.ReadPayload<RejectMessage>(reply);
        await channel.CloseAsync().ConfigureAwait(false);
        return new JoinResult(false, reject?.Reason ?? JoinRejectReason.BadRequest,
            string.IsNullOrEmpty(reject?.Message) ? "The host refused the join." : reject!.Message);
    }

    private static JoinRejectReason ReasonFor(HandshakeFailure failure) => failure switch
    {
        HandshakeFailure.BadPassword => JoinRejectReason.WrongPassword,
        HandshakeFailure.PasswordRequired => JoinRejectReason.WrongPassword,
        HandshakeFailure.RateLimited => JoinRejectReason.TooManyAttempts,
        HandshakeFailure.Incompatible => JoinRejectReason.VersionMismatch,
        _ => JoinRejectReason.BadRequest
    };

    private void OnChannelClosed(NetChannel channel)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_channel, channel) || _intentional) return;
            _channel = null;
            if (_reconnecting) return;
            if (_options.AutoReconnect) _reconnecting = true;
        }

        if (!_options.AutoReconnect)
        {
            SetState(ClientState.Disconnected);
            return;
        }

        SetState(ClientState.Reconnecting);
        _ = Task.Run(ReconnectLoopAsync);
    }

    private async Task ReconnectLoopAsync()
    {
        var token = _lifetime.Token;
        int attempt = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var delays = _options.ReconnectDelays;
                var delay = delays.Length == 0 ? TimeSpan.FromSeconds(1) : delays[Math.Min(attempt, delays.Length - 1)];
                attempt++;
                await Task.Delay(delay, token).ConfigureAwait(false);
                if (_intentional) return;   // kicked / closed / left while we were waiting

                try
                {
                    var result = await ConnectOnceAsync(resume: true).ConfigureAwait(false);
                    if (result.Accepted && _intentional)
                    {
                        NetChannel? stray;
                        lock (_gate) { stray = _channel; _channel = null; }
                        if (stray != null) await stray.CloseAsync().ConfigureAwait(false);
                        return;
                    }

                    if (result.Accepted)
                    {
                        bool lostAgain;
                        lock (_gate)
                        {
                            lostAgain = _channel == null && !_intentional;   // dropped again before we could settle
                            if (!lostAgain) _reconnecting = false;
                        }

                        if (lostAgain)
                        {
                            attempt = 0;
                            continue;
                        }

                        SetState(ClientState.Connected);
                        return;
                    }

                    LastMessage = result.Message;   // kicked, session closed, version change... stop trying
                    SetState(result.Reason == JoinRejectReason.Kicked ? ClientState.Kicked : ClientState.Rejected);
                    return;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    NetLog.Warn($"reconnect attempt {attempt} failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_gate) _reconnecting = false;
        }
    }

    private void StartWatchdog()
    {
        lock (_gate)
        {
            if (_watchdogStarted) return;
            _watchdogStarted = true;
        }
        _ = Task.Run(WatchdogLoopAsync);
    }

    // The host pings every few seconds; a long silence means the link is dead even though the socket looks open.
    private async Task WatchdogLoopAsync()
    {
        var token = _lifetime.Token;
        int interval = (int)Math.Max(50, _options.HostSilenceTimeout.TotalMilliseconds / 4);
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, token).ConfigureAwait(false);

                NetChannel? channel;
                lock (_gate) channel = _channel;
                if (channel != null && DateTime.UtcNow - channel.LastReceivedUtc > _options.HostSilenceTimeout)
                {
                    NetLog.Warn("the host went silent; dropping the link");
                    await channel.CloseAsync().ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    // ---------------------------------------------------------------- host traffic

    private async Task OnHostMessageAsync(NetChannel channel, Envelope envelope)
    {
        switch (envelope.Type)
        {
            case NetMessageTypes.Ping:
                await channel.TrySendAsync(NetMessageTypes.Pong, new PingMessage { Ticks = DateTime.UtcNow.Ticks }, envelope.Id).ConfigureAwait(false);
                break;

            case NetMessageTypes.Roster:
                var roster = EnvelopeCodec.ReadPayload<RosterMessage>(envelope);
                if (roster != null)
                {
                    Roster = roster;
                    Raise(() => RosterChanged?.Invoke(roster));
                }
                break;

            case NetMessageTypes.KickNotice:
                await EndAsync(channel, ClientState.Kicked, envelope).ConfigureAwait(false);
                break;

            case NetMessageTypes.SessionClosing:
                await EndAsync(channel, ClientState.SessionClosed, envelope).ConfigureAwait(false);
                break;

            default:
                Raise(() => MessageReceived?.Invoke(envelope));
                break;
        }
    }

    private async Task EndAsync(NetChannel channel, ClientState finalState, Envelope notice)
    {
        lock (_gate) _intentional = true;
        LastMessage = EnvelopeCodec.ReadPayload<NoticeMessage>(notice)?.Message ?? string.Empty;
        SetState(finalState);
        await channel.CloseAsync().ConfigureAwait(false);
    }

    private NetChannel RequireChannel()
    {
        lock (_gate)
            return _channel ?? throw new IOException("Not connected to the host.");
    }

    private void SetState(ClientState state)
    {
        lock (_gate)
        {
            if (_state == state) return;
            _state = state;
        }
        NetLog.Info($"client state: {state}");
        Raise(() => StateChanged?.Invoke(state));
    }

    private static void Raise(Action action)
    {
        try { action(); }
        catch (Exception ex) { NetLog.Error("event handler failed", ex); }
    }
}