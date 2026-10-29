using System.IO.Compression;
using System.Text;

namespace CharManJur.Services;

// Packs a character's JSON for the wire (gzip) and unpacks it again with a hard size limit, so a hostile
// player cannot send a few kilobytes that expand into gigabytes.
public static class SnapshotCodec
{
    // What one frame can carry is FrameCodec.MaxFrameBytes (1 MB), and the packed bytes travel as base64 inside JSON.
    public const int MaxPackedBytes = 600 * 1024;
    public const int MaxJsonBytes = 16 * 1024 * 1024;

    // Throws InvalidDataException when the character is too large to send.
    public static byte[] Pack(string json)
    {
        byte[] raw = Encoding.UTF8.GetBytes(json);
        if (raw.Length > MaxJsonBytes) throw new InvalidDataException("The character is too large to send.");

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(raw, 0, raw.Length);

        if (output.Length > MaxPackedBytes) throw new InvalidDataException("The character is too large to send.");
        return output.ToArray();
    }

    // Throws InvalidDataException for anything that is not valid gzip, or expands past the limit.
    public static string Unpack(byte[] packed)
    {
        if (packed == null || packed.Length == 0) throw new InvalidDataException("The character copy is empty.");
        if (packed.Length > MaxPackedBytes) throw new InvalidDataException("The character copy is too large.");

        try
        {
            using var input = new MemoryStream(packed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            var buffer = new byte[16 * 1024];
            int read;
            while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + read > MaxJsonBytes) throw new InvalidDataException("The character copy expands past the size limit.");
                output.Write(buffer, 0, read);
            }
            return Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
        }
        catch (InvalidDataException) { throw; }
        catch (Exception ex) when (ex is IOException or NotSupportedException)
        {
            throw new InvalidDataException("The character copy could not be read.", ex);
        }
    }
}