using CharManJur.Services;

namespace CharManJur.Models;

// Wire payloads for the session layer. Plain data only (no MAUI types) so the network core can be unit-tested.
// Every payload type must also be listed in NetJsonContext.

public enum JoinRejectReason
{
    BadRequest,
    VersionMismatch,
    Closed,
    Full,
    DuplicateCharacter,
    WrongPassword,
    PolicyRefused,
    Kicked,
    TooManyAttempts
}

public sealed class HelloMessage
{
    public string AppVersion { get; set; } = string.Empty;
    public int ProtocolVersion { get; set; } = NetProtocol.Version;
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Guid CharacterGuid { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public bool Resume { get; set; }

    // Fingerprints of the character as saved on the player's device, so the GM can tell whether it changed since the
    // last copy the GM stored (no character data travels in the hello). Both optional.
    public string? DataHash { get; set; }
    public Dictionary<string, string>? SectionHashes { get; set; }   // one short hash per top-level section of the character
}

public sealed class AcceptMessage
{
    public string SessionName { get; set; } = string.Empty;
    public int HeartbeatSeconds { get; set; }
    public bool Resumed { get; set; }
}

public sealed class RejectMessage
{
    public JoinRejectReason Reason { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class PingMessage
{
    public long Ticks { get; set; }
}

public sealed class NoticeMessage
{
    public string Message { get; set; } = string.Empty;
}

public sealed class AckMessage
{
    public bool Ok { get; set; } = true;
    public string? Message { get; set; }
}

public sealed class RosterEntry
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Guid CharacterGuid { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public bool Connected { get; set; }
}

public sealed class RosterMessage
{
    public string SessionName { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public List<RosterEntry> Players { get; set; } = new();
}


// A full copy of one character, gzip-compressed JSON (the app builds and reads the JSON; the network core only carries it).
public sealed class CharacterSnapshotMessage
{
    public Guid CharacterGuid { get; set; }
    public string Reason { get; set; } = string.Empty;   // "leaving", "session closing"
    public byte[] GzipJson { get; set; } = Array.Empty<byte>();
}

// The GM asks a player for a fresh copy of their character.
public sealed class SnapshotRequestMessage
{
    public string Reason { get; set; } = string.Empty;
}

// One live stat on the GM's player card ("HP", "12/20", level 0 = full, 1 = below its max, 2 = gone).
public sealed class SummaryChip
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Level { get; set; }
}

// What the GM's card for a player shows. Small and frequent: only the chips, never the character.
public sealed class CharacterSummaryMessage
{
    public Guid CharacterGuid { get; set; }
    public List<SummaryChip> Chips { get; set; } = new();
    public string Compact { get; set; } = string.Empty;
    public int Waiting { get; set; }   // damage / effects sent by the GM that the player has not taken on yet
}

// Something the GM does to a player's character. The core only carries it: Kind says what it is
// ("damage", "effect", ...) and Json is the app's own description, which the player's app applies.
public sealed class GmActionMessage
{
    public Guid ActionId { get; set; }   // the same id on every resend, so the player can tell a repeat from a new action
    public string Kind { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}