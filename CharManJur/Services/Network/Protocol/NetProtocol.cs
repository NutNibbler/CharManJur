namespace CharManJur.Services;

public static class NetProtocol
{
    // Bumped whenever the wire format changes incompatibly. Checked in the handshake.
    public const int Version = 1;

    public const int DefaultPort = 47200;
    public const int DiscoveryPort = 47201;
}

// Envelope.Type values. New message kinds are only ever ADDED; unknown types are ignored by receivers.
public static class NetMessageTypes
{
    // Handshake
    public const string Hello = "hello";
    public const string Accept = "accept";
    public const string Reject = "reject";

    // Session
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Roster = "roster";
    public const string KickNotice = "kick";
    public const string SessionClosing = "session.closing";
    public const string Leave = "leave";

    // Character copies
    public const string CharacterSnapshot = "char.snapshot";               // player -> GM: a full copy of the character (also the reply to the request below)
    public const string SnapshotRequest = "char.snapshot.request";         // GM -> player: save and send me your character now
    public const string CharacterSummary = "char.summary";                 // player -> GM: the live stat chips (HP, Vigor...), sent whenever they change

    // GM actions
    public const string GmAction = "gm.action";                            // GM -> player: damage / effect the player's app queues; the reply is an Ack

    // Generic
    public const string Ack = "ack";
}