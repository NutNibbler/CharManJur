using System.Text;

namespace CharManJur.Services;

// What a GM's app shouts on the local network so players can find the session. It is only a hint: anyone on the
// network can send one, so it carries nothing private (no character data, no keys) and the player still has to
// pass the encrypted handshake and password before anything real happens.
public sealed record SessionAnnouncement(
    Guid SessionId,
    string SessionName,
    int TcpPort,
    bool PasswordRequired,
    bool IsOpen,
    int PlayerCount,
    int MaxPlayers)
{
    public const int MaxNameChars = 40;

    private static readonly byte[] Magic = { (byte)'C', (byte)'M', (byte)'J', (byte)'D' };
    private const byte Version = 1;

    public byte[] Encode()
    {
        var name = SessionName.Length > MaxNameChars ? SessionName[..MaxNameChars] : SessionName;
        var nameBytes = Encoding.UTF8.GetBytes(name);
        // The name can be at most 40 chars; clip bytes too in case of long characters
        if (nameBytes.Length > 120) nameBytes = nameBytes[..120];

        var frame = new List<byte>(40 + nameBytes.Length);
        frame.AddRange(Magic);
        frame.Add(Version);
        frame.Add((byte)((PasswordRequired ? 1 : 0) | (IsOpen ? 2 : 0)));
        frame.AddRange(SessionId.ToByteArray());
        frame.Add((byte)(TcpPort >> 8));
        frame.Add((byte)TcpPort);
        frame.Add((byte)Math.Clamp(PlayerCount, 0, 255));
        frame.Add((byte)Math.Clamp(MaxPlayers, 0, 255));
        frame.Add((byte)nameBytes.Length);
        frame.AddRange(nameBytes);
        return frame.ToArray();
    }

    // Null for anything that is not a well-formed announcement (a datagram is untrusted input).
    public static SessionAnnouncement? TryDecode(ReadOnlySpan<byte> data)
    {
        const int fixedPart = 4 + 1 + 1 + 16 + 2 + 1 + 1 + 1;
        if (data.Length < fixedPart || data.Length > fixedPart + 120) return null;
        if (!data[..4].SequenceEqual(Magic) || data[4] != Version) return null;

        byte flags = data[5];
        var id = new Guid(data.Slice(6, 16));
        int port = (data[22] << 8) | data[23];
        int players = data[24];
        int max = data[25];
        int nameLength = data[26];
        if (port == 0 || data.Length != fixedPart + nameLength) return null;

        string name;
        try { name = new UTF8Encoding(false, true).GetString(data.Slice(fixedPart, nameLength)); }
        catch (ArgumentException) { return null; }

        // Control characters would be shown to players; drop them
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (name.Length == 0 || name.Length > MaxNameChars) return null;

        return new SessionAnnouncement(id, name, port, (flags & 1) != 0, (flags & 2) != 0, players, max);
    }
}