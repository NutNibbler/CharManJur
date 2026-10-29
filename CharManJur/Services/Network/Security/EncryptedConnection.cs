namespace CharManJur.Services;

// Wraps a raw connection so every frame is sealed with the direction's key and a counter nonce.
// A frame that fails to open ends the link: it was altered, replayed, duplicated, dropped before it or out of order.
internal sealed class EncryptedConnection : IConnection
{
    private readonly IConnection _inner;
    private readonly Aead _send;
    private readonly Aead _receive;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ulong _sendCounter;
    private ulong _receiveCounter;

    public EncryptedConnection(IConnection inner, Aead send, Aead receive, ulong sendCounter, ulong receiveCounter)
    {
        _inner = inner;
        _send = send;
        _receive = receive;
        _sendCounter = sendCounter;
        _receiveCounter = receiveCounter;
    }

    public string RemoteDescription => _inner.RemoteDescription;
    public bool IsOpen => _inner.IsOpen;

    public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // The counter and the order on the wire must match, so both happen under one lock.
            var sealedFrame = _send.Seal(_sendCounter, frame.Span);
            await _inner.SendAsync(sealedFrame, ct).ConfigureAwait(false);
            _sendCounter++;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<byte[]?> ReceiveAsync(CancellationToken ct = default)
    {
        var frame = await _inner.ReceiveAsync(ct).ConfigureAwait(false);
        if (frame == null) return null;

        var plaintext = _receive.Open(_receiveCounter, frame);
        if (plaintext == null)
        {
            NetLog.Warn($"a frame from {RemoteDescription} failed to decrypt; closing the link");
            await _inner.CloseAsync().ConfigureAwait(false);
            return null;
        }

        _receiveCounter++;
        return plaintext;
    }

    public Task CloseAsync() => _inner.CloseAsync();

    public ValueTask DisposeAsync()
    {
        _ = _inner.DisposeAsync();
        return ValueTask.CompletedTask;
    }
}