using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CharManJur.Services;

// In-memory transport: host and any number of clients inside ONE process. Addresses are plain names.
// Used for tests, the Network Lab and the in-app test players, so the whole protocol runs on one device.
public sealed class LoopbackTransport : ITransport
{
    private readonly ConcurrentDictionary<string, LoopbackListener> _listeners = new(StringComparer.OrdinalIgnoreCase);

    public Task<IListener> ListenAsync(string address, CancellationToken ct = default)
    {
        var listener = new LoopbackListener(address, a => _listeners.TryRemove(a, out _));
        if (!_listeners.TryAdd(address, listener))
            throw new IOException($"Loopback address '{address}' is already in use.");
        return Task.FromResult<IListener>(listener);
    }

    public Task<IConnection> ConnectAsync(string address, CancellationToken ct = default)
    {
        if (!_listeners.TryGetValue(address, out var listener) || listener.IsStopped)
            throw new IOException($"Nothing is listening on loopback address '{address}'.");

        var clientToServer = Channel.CreateUnbounded<byte[]>();
        var serverToClient = Channel.CreateUnbounded<byte[]>();

        var client = new LoopbackConnection("loopback:" + address, serverToClient, clientToServer);
        var server = new LoopbackConnection("loopback:client", clientToServer, serverToClient);

        if (!listener.Enqueue(server))
            throw new IOException($"Nothing is listening on loopback address '{address}'.");
        return Task.FromResult<IConnection>(client);
    }

    private sealed class LoopbackListener : IListener
    {
        private readonly string _address;
        private readonly Action<string> _onStopped;
        private readonly Channel<IConnection> _queue = Channel.CreateUnbounded<IConnection>();

        public LoopbackListener(string address, Action<string> onStopped)
        {
            _address = address;
            _onStopped = onStopped;
        }

        public bool IsStopped { get; private set; }
        public string LocalDescription => "loopback:" + _address;

        public bool Enqueue(IConnection connection) => _queue.Writer.TryWrite(connection);

        public async Task<IConnection?> AcceptAsync(CancellationToken ct = default)
        {
            try { return await _queue.Reader.ReadAsync(ct).ConfigureAwait(false); }
            catch (ChannelClosedException) { return null; }
        }

        public void Stop()
        {
            if (IsStopped) return;
            IsStopped = true;
            _onStopped(_address);
            _queue.Writer.TryComplete();
        }

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class LoopbackConnection : IConnection
    {
        private readonly Channel<byte[]> _in;
        private readonly Channel<byte[]> _out;
        private volatile bool _closed;

        public LoopbackConnection(string remote, Channel<byte[]> inbound, Channel<byte[]> outbound)
        {
            RemoteDescription = remote;
            _in = inbound;
            _out = outbound;
        }

        public string RemoteDescription { get; }
        public bool IsOpen => !_closed && !_in.Reader.Completion.IsCompleted;

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
        {
            if (_closed) throw new IOException("Connection is closed.");
            if (!_out.Writer.TryWrite(frame.ToArray())) throw new IOException("The other side has closed the connection.");
            return Task.CompletedTask;
        }

        public async Task<byte[]?> ReceiveAsync(CancellationToken ct = default)
        {
            if (_closed) return null;
            try { return await _in.Reader.ReadAsync(ct).ConfigureAwait(false); }
            catch (ChannelClosedException) { return null; }
        }

        public Task CloseAsync()
        {
            _closed = true;
            _out.Writer.TryComplete();   // the peer drains what is queued, then sees the end
            _in.Writer.TryComplete();    // the peer's further sends fail; a pending read here ends
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _ = CloseAsync();
            return ValueTask.CompletedTask;
        }
    }
}