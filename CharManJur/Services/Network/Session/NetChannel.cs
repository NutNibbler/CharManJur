using System.Collections.Concurrent;
using System.Threading.Channels;
using CharManJur.Models;

namespace CharManJur.Services;

// Envelope-level link over one IConnection: sequencing, request/response correlation, ordered dispatch.
// Replies are matched on the read loop; everything else is queued and handled one at a time on a separate loop,
// so a handler may itself await a request on this channel without deadlocking.
// ALL callbacks arrive on pool threads: marshal to the UI thread before touching bound state.
public sealed class NetChannel : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly Guid _selfId;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Envelope>> _pending = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<Envelope> _inbox = Channel.CreateUnbounded<Envelope>(new UnboundedChannelOptions { SingleReader = true });

    private long _seq;
    private long _lastReceivedTicks;
    private int _closedFlag;
    private Func<Envelope, Task>? _handler;

    public NetChannel(IConnection connection, Guid selfId)
    {
        _connection = connection;
        _selfId = selfId;
        _lastReceivedTicks = DateTime.UtcNow.Ticks;
    }

    public string RemoteDescription => _connection.RemoteDescription;
    public bool IsOpen => Volatile.Read(ref _closedFlag) == 0 && _connection.IsOpen;
    public DateTime LastReceivedUtc => new(Interlocked.Read(ref _lastReceivedTicks), DateTimeKind.Utc);

    // Handles every incoming envelope that is not a reply to one of our requests. May be swapped at any time.
    public Func<Envelope, Task>? Handler
    {
        get => Volatile.Read(ref _handler);
        set => Volatile.Write(ref _handler, value);
    }

    // Raised once, when the link ends for any reason.
    public event Action<NetChannel>? Closed;

    public void Start()
    {
        _ = Task.Run(ReadLoopAsync);
        _ = Task.Run(DispatchLoopAsync);
    }

    public Task SendAsync<T>(string type, T payload, Guid? corr = null)
        => SendEnvelopeAsync(EnvelopeCodec.Create(type, payload, corr));

    public Task ReplyAsync<T>(Envelope request, string type, T payload)
        => SendAsync(type, payload, request.Id);

    public async Task<bool> TrySendAsync<T>(string type, T payload, Guid? corr = null)
    {
        try
        {
            await SendAsync(type, payload, corr).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            return false;
        }
    }

    // Sends and waits for the envelope whose Corr equals this request's Id.
    // Throws TimeoutException, or IOException if the link closes first.
    public async Task<Envelope> RequestAsync<T>(string type, T payload, TimeSpan timeout)
    {
        var envelope = EnvelopeCodec.Create(type, payload);
        var tcs = new TaskCompletionSource<Envelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[envelope.Id] = tcs;
        try
        {
            await SendEnvelopeAsync(envelope).ConfigureAwait(false);

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var registration = timeoutCts.Token.Register(() =>
                tcs.TrySetException(new TimeoutException($"No reply to '{type}' within {timeout.TotalSeconds:0.##}s.")));
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _pending.TryRemove(envelope.Id, out _);
        }
    }

    public async Task SendEnvelopeAsync(Envelope envelope)
    {
        if (Volatile.Read(ref _closedFlag) == 1) throw new IOException("Channel is closed.");

        try
        {
            await _sendLock.WaitAsync(_cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new IOException("Channel is closed.");
        }

        try
        {
            envelope.From = _selfId;
            envelope.Seq = Interlocked.Increment(ref _seq);
            await _connection.SendAsync(EnvelopeCodec.Encode(envelope), _cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new IOException("Channel is closed.");
        }
        catch (IOException)
        {
            _ = ShutdownAsync();
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public Task CloseAsync() => ShutdownAsync();

    public ValueTask DisposeAsync() => new(ShutdownAsync());

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                byte[]? frame = await _connection.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                if (frame == null) break;

                var envelope = EnvelopeCodec.Decode(frame);
                if (envelope == null)
                {
                    NetLog.Warn("dropped an undecodable frame");
                    continue;
                }

                Interlocked.Exchange(ref _lastReceivedTicks, DateTime.UtcNow.Ticks);

                if (envelope.Corr is Guid corr && _pending.TryRemove(corr, out var waiting))
                {
                    waiting.TrySetResult(envelope);
                    continue;
                }
                _inbox.Writer.TryWrite(envelope);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { NetLog.Error("read loop failed", ex); }
        finally
        {
            await ShutdownAsync().ConfigureAwait(false);
        }
    }

    private async Task DispatchLoopAsync()
    {
        await foreach (var envelope in _inbox.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            var handler = Handler;
            if (handler == null)
            {
                NetLog.Warn($"no handler for '{envelope.Type}'");
                continue;
            }

            try { await handler(envelope).ConfigureAwait(false); }
            catch (Exception ex) { NetLog.Error($"handler for '{envelope.Type}' failed", ex); }
        }
    }

    private async Task ShutdownAsync()
    {
        if (Interlocked.Exchange(ref _closedFlag, 1) == 1) return;

        _cts.Cancel();
        _inbox.Writer.TryComplete();
        foreach (var waiting in _pending.Values)
            waiting.TrySetException(new IOException("Connection closed."));

        try { await _connection.CloseAsync().ConfigureAwait(false); } catch { }

        try { Closed?.Invoke(this); }
        catch (Exception ex) { NetLog.Error("Closed handler failed", ex); }
    }
}