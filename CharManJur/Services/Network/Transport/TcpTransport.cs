using System.Net;
using System.Net.Sockets;

namespace CharManJur.Services;

// Real network transport. Addresses: "host:port", ":port" or just "port" (IPv4; default port 47200).
// Listening: the host part must be an IP address (blank = all interfaces). Port 0 lets the OS pick (used by tests).
public sealed class TcpTransport : ITransport
{
    public Task<IListener> ListenAsync(string address, CancellationToken ct = default)
    {
        var (host, port) = ParseAddress(address);
        IPAddress ip = string.IsNullOrEmpty(host) ? IPAddress.Any : IPAddress.Parse(host);

        var listener = new TcpListener(ip, port);
        listener.Start();
        return Task.FromResult<IListener>(new TcpListenerAdapter(listener));
    }

    public async Task<IConnection> ConnectAsync(string address, CancellationToken ct = default)
    {
        var (host, port) = ParseAddress(address);
        if (string.IsNullOrEmpty(host)) host = "127.0.0.1";

        var client = new TcpClient { NoDelay = true };
        try
        {
            await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }
        return new TcpConnectionAdapter(client);
    }

    internal static (string Host, int Port) ParseAddress(string address)
    {
        string text = (address ?? string.Empty).Trim();
        if (text.Length == 0) return (string.Empty, NetProtocol.DefaultPort);

        if (int.TryParse(text, out int onlyPort)) return (string.Empty, onlyPort);

        int colon = text.LastIndexOf(':');
        if (colon < 0) return (text, NetProtocol.DefaultPort);

        string host = text[..colon];
        string portText = text[(colon + 1)..];
        int port = portText.Length == 0 ? NetProtocol.DefaultPort : int.Parse(portText);
        return (host, port);
    }

    private sealed class TcpListenerAdapter : IListener
    {
        private readonly TcpListener _listener;
        private volatile bool _stopped;

        public TcpListenerAdapter(TcpListener listener) => _listener = listener;

        public string LocalDescription => _listener.LocalEndpoint.ToString() ?? string.Empty;

        public async Task<IConnection?> AcceptAsync(CancellationToken ct = default)
        {
            while (!_stopped)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    client.NoDelay = true;
                    return new TcpConnectionAdapter(client);
                }
                catch (ObjectDisposedException) { return null; }
                catch (SocketException ex)
                {
                    if (_stopped) return null;
                    NetLog.Warn($"accept failed: {ex.SocketErrorCode}");   // a bad handshake attempt; keep listening
                }
            }
            return null;
        }

        public void Stop()
        {
            _stopped = true;
            try { _listener.Stop(); } catch { }
        }

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TcpConnectionAdapter : IConnection
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private volatile bool _closed;

        public TcpConnectionAdapter(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            string remote;
            try { remote = client.Client.RemoteEndPoint?.ToString() ?? "?"; } catch { remote = "?"; }
            RemoteDescription = remote;
        }

        public string RemoteDescription { get; }
        public bool IsOpen => !_closed;

        public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
        {
            if (_closed) throw new IOException("Connection is closed.");

            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await FrameCodec.WriteFrameAsync(_stream, frame, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                MarkClosed();
                throw new IOException("Send failed: " + ex.Message, ex);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task<byte[]?> ReceiveAsync(CancellationToken ct = default)
        {
            if (_closed) return null;
            try
            {
                var frame = await FrameCodec.ReadFrameAsync(_stream, ct).ConfigureAwait(false);
                if (frame == null) MarkClosed();
                return frame;
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or InvalidDataException)
            {
                if (ex is InvalidDataException) NetLog.Warn("closing connection: " + ex.Message);
                MarkClosed();
                return null;
            }
        }

        public Task CloseAsync()
        {
            MarkClosed();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            MarkClosed();
            return ValueTask.CompletedTask;
        }

        private void MarkClosed()
        {
            if (_closed) return;
            _closed = true;
            try { _client.Close(); } catch { }
        }
    }
}