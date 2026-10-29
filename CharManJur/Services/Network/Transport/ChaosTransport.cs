namespace CharManJur.Services;

// Live-tunable fault settings. All properties can be changed while connections are open (the Network Lab does).
public sealed class ChaosOptions
{
    public int LatencyMs { get; set; }
    public int JitterMs { get; set; }
    public double DropRate { get; set; }          // 0..1, sent frames silently lost
    public double DuplicateRate { get; set; }     // 0..1, sent frames delivered twice
    public int? DisconnectAfterFrames { get; set; } // per connection: the Nth+1 send kills the link
    public int Seed { get; set; } = 12345;
}

// Wraps any transport and injects latency, loss, duplicates and disconnects, so reconnect / freeze / dedupe
// logic can be exercised on one machine. Real TCP never silently drops a frame, so DropRate models a lost ack.
public sealed class ChaosTransport : ITransport
{
    private readonly ITransport _inner;
    private readonly ChaosOptions _options;
    private readonly List<ChaosConnection> _connections = new();
    private int _counter;

    public ChaosTransport(ITransport inner, ChaosOptions? options = null)
    {
        _inner = inner;
        _options = options ?? new ChaosOptions();
    }

    public ChaosOptions Options => _options;

    public async Task<IListener> ListenAsync(string address, CancellationToken ct = default)
        => new ChaosListener(await _inner.ListenAsync(address, ct).ConfigureAwait(false), this);

    public async Task<IConnection> ConnectAsync(string address, CancellationToken ct = default)
        => Wrap(await _inner.ConnectAsync(address, ct).ConfigureAwait(false));

    // Kills every link made through this transport (simulates a Wi-Fi drop).
    public async Task DropAllConnectionsAsync()
    {
        List<ChaosConnection> snapshot;
        lock (_connections) snapshot = _connections.ToList();
        foreach (var c in snapshot) await c.CloseAsync().ConfigureAwait(false);
    }

    public IConnection Wrap(IConnection inner)
    {
        var wrapped = new ChaosConnection(inner, _options, _options.Seed + Interlocked.Increment(ref _counter));
        lock (_connections)
        {
            _connections.RemoveAll(c => !c.IsOpen);
            _connections.Add(wrapped);
        }
        return wrapped;
    }

    private sealed class ChaosListener : IListener
    {
        private readonly IListener _inner;
        private readonly ChaosTransport _owner;

        public ChaosListener(IListener inner, ChaosTransport owner)
        {
            _inner = inner;
            _owner = owner;
        }

        public string LocalDescription => _inner.LocalDescription;

        public async Task<IConnection?> AcceptAsync(CancellationToken ct = default)
        {
            var connection = await _inner.AcceptAsync(ct).ConfigureAwait(false);
            return connection == null ? null : _owner.Wrap(connection);
        }

        public void Stop() => _inner.Stop();
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }

    private sealed class ChaosConnection : IConnection
    {
        private readonly IConnection _inner;
        private readonly ChaosOptions _options;
        private readonly Random _random;
        private int _sent;

        public ChaosConnection(IConnection inner, ChaosOptions options, int seed)
        {
            _inner = inner;
            _options = options;
            _random = new Random(seed);
        }

        public string RemoteDescription => _inner.RemoteDescription;
        public bool IsOpen => _inner.IsOpen;

        public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
        {
            var o = _options;

            int delay = o.LatencyMs + (o.JitterMs > 0 ? Next(o.JitterMs + 1) : 0);
            if (delay > 0) await Task.Delay(delay, ct).ConfigureAwait(false);

            int count = Interlocked.Increment(ref _sent);
            if (o.DisconnectAfterFrames is int limit && count > limit)
            {
                await _inner.CloseAsync().ConfigureAwait(false);
                throw new IOException("Chaos: forced disconnect.");
            }

            if (o.DropRate > 0 && NextDouble() < o.DropRate) return;

            await _inner.SendAsync(frame, ct).ConfigureAwait(false);

            if (o.DuplicateRate > 0 && NextDouble() < o.DuplicateRate)
                await _inner.SendAsync(frame, ct).ConfigureAwait(false);
        }

        public Task<byte[]?> ReceiveAsync(CancellationToken ct = default) => _inner.ReceiveAsync(ct);
        public Task CloseAsync() => _inner.CloseAsync();
        public ValueTask DisposeAsync() => _inner.DisposeAsync();

        private int Next(int max) { lock (_random) return _random.Next(max); }
        private double NextDouble() { lock (_random) return _random.NextDouble(); }
    }
}