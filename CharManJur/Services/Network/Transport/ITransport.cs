namespace CharManJur.Services;

// One open link to a peer. Frames in, frames out. TCP, loopback and (later) WebRTC all look like this.
public interface IConnection : IAsyncDisposable
{
    string RemoteDescription { get; }
    bool IsOpen { get; }

    // Throws IOException when the connection is closed or the write fails.
    Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default);

    // The next frame, or null once the connection has closed (cleanly or not).
    Task<byte[]?> ReceiveAsync(CancellationToken ct = default);

    Task CloseAsync();
}

public interface IListener : IAsyncDisposable
{
    // Where this listener actually is (e.g. "0.0.0.0:47200"). With port 0 this shows the port the OS picked.
    string LocalDescription { get; }

    // The next incoming connection, or null once the listener has been stopped.
    Task<IConnection?> AcceptAsync(CancellationToken ct = default);

    void Stop();
}

public interface ITransport
{
    Task<IListener> ListenAsync(string address, CancellationToken ct = default);
    Task<IConnection> ConnectAsync(string address, CancellationToken ct = default);
}