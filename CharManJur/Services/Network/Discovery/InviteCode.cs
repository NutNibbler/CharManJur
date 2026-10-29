using System.Net;
using System.Net.Sockets;

namespace CharManJur.Services;

// A short code a GM can read out or text to a player instead of an IP address: 12 characters like "7QK4-M2XD-9HTA".
// It carries the GM's IPv4 address, the port, whether a password is needed, and a check value so a mistyped
// character is caught here instead of becoming a confusing "could not connect".
// The alphabet leaves out I, L, O and U so nothing looks like a 1, a 0 or another letter.
public static class InviteCode
{
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";   // 32 characters, Crockford-style
    private const int Length = 12;

    public sealed record Decoded(IPAddress Address, int Port, bool PasswordRequired)
    {
        public string HostAddress => $"{Address}:{Port}";   // what TcpTransport connects to
    }

    public static string Encode(IPAddress address, int port, bool passwordRequired)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Invite codes hold IPv4 addresses only.", nameof(address));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));

        var ip = address.GetAddressBytes();
        // 4 flag bits (bit 0 = password), 32 address bits, 16 port bits, 8 check bits = 60 bits = 12 characters
        ulong data = (passwordRequired ? 1UL : 0UL);
        foreach (var b in ip) data = (data << 8) | b;
        data = (data << 16) | (uint)port;           // 4 + 32 + 16 = 52 bits
        ulong all = (data << 8) | Check(data);      // 60 bits

        var chars = new char[Length];
        for (int i = Length - 1; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(all & 31)];
            all >>= 5;
        }
        var text = new string(chars);
        return $"{text[..4]}-{text[4..8]}-{text[8..]}";
    }

    // Forgiving about case, spaces, dashes and look-alike characters (O for 0, I or L for 1). Null when it is not a valid code.
    public static Decoded? TryDecode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        ulong all = 0;
        int count = 0;
        foreach (var raw in text)
        {
            if (raw is '-' or ' ' or '_' || char.IsWhiteSpace(raw)) continue;
            var c = char.ToUpperInvariant(raw);
            c = c switch { 'O' => '0', 'I' => '1', 'L' => '1', _ => c };

            int value = Alphabet.IndexOf(c);
            if (value < 0 || ++count > Length) return null;
            all = (all << 5) | (uint)value;
        }
        if (count != Length) return null;

        ulong data = all >> 8;
        if ((byte)(all & 0xFF) != Check(data)) return null;

        int port = (int)(data & 0xFFFF);
        var ipValue = (uint)((data >> 16) & 0xFFFFFFFF);
        bool password = ((data >> 48) & 1) != 0;
        if (port == 0) return null;

        var address = new IPAddress(new[] { (byte)(ipValue >> 24), (byte)(ipValue >> 16), (byte)(ipValue >> 8), (byte)ipValue });
        return new Decoded(address, port, password);
    }

    // CRC-8 (polynomial 0x07) over the 52 data bits, as 7 bytes
    private static byte Check(ulong data)
    {
        byte crc = 0;
        for (int i = 6; i >= 0; i--)
        {
            crc ^= (byte)(data >> (i * 8));
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
        }
        return crc;
    }
}