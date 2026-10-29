using System.Buffers.Binary;

namespace CharManJur.Services;

// Wire framing: 4-byte big-endian length, then that many payload bytes.
public static class FrameCodec
{
    public const int MaxFrameBytes = 1024 * 1024;

    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (payload.Length > MaxFrameBytes)
            throw new InvalidDataException($"Frame of {payload.Length} bytes exceeds the {MaxFrameBytes} byte limit.");

        var buffer = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(buffer, payload.Length);
        payload.CopyTo(buffer.AsMemory(4));

        await stream.WriteAsync(buffer, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    // Returns null when the stream ended cleanly between frames.
    // Throws EndOfStreamException for a frame cut short, InvalidDataException for an impossible length.
    public static async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[4];
        int got = await ReadExactAsync(stream, header, ct).ConfigureAwait(false);
        if (got == 0) return null;
        if (got < 4) throw new EndOfStreamException("Connection ended inside a frame header.");

        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length < 0 || length > MaxFrameBytes)
            throw new InvalidDataException($"Frame length {length} is outside 0..{MaxFrameBytes}.");

        var payload = new byte[length];
        if (length > 0)
        {
            int read = await ReadExactAsync(stream, payload, ct).ConfigureAwait(false);
            if (read < length) throw new EndOfStreamException("Connection ended inside a frame.");
        }
        return payload;
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(total), ct).ConfigureAwait(false);
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}