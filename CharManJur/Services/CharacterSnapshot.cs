using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

// Turns a saved character into the message that travels to the GM, and back, and fingerprints it for the
// "did it change while they were away" check. The network core only carries the bytes.
public static class CharacterSnapshots
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // Must stay the same as the options CampaignStore hashes with, so the two fingerprints can be compared.
    private static readonly JsonSerializerOptions HashOptions = new() { PropertyNameCaseInsensitive = true };

    // Throws InvalidDataException when the character is too large to send.
    public static CharacterSnapshotMessage Pack(CharacterSaveData save, string reason) => new()
    {
        CharacterGuid = save.CharacterGuid,
        Reason = reason,
        GzipJson = SnapshotCodec.Pack(JsonSerializer.Serialize(save, Options))
    };

    // Throws InvalidDataException / JsonException for a damaged copy.
    public static CharacterSaveData? Unpack(CharacterSnapshotMessage message)
        => JsonSerializer.Deserialize<CharacterSaveData>(SnapshotCodec.Unpack(message.GzipJson), Options);

    // The same fingerprint CampaignStore keeps when it clones a character.
    public static string HashData(CharacterData data)
    {
        var roundTripped = RoundTrip(data);
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(roundTripped, HashOptions));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    // One short hash per top-level section ("Inventory", "HitpointsDamage"...), so the GM can say WHAT changed
    // without the character itself having to travel.
    public static Dictionary<string, string> SectionHashes(CharacterData data)
    {
        var node = JsonSerializer.SerializeToNode(RoundTrip(data), HashOptions)!.AsObject();
        return node.ToDictionary(p => p.Key, p => ShortHash(p.Value?.ToJsonString() ?? "null"));
    }

    // The sections that differ between a stored copy and the sections a returning player reported.
    public static List<string> ChangedSections(CharacterData stored, IReadOnlyDictionary<string, string>? reported)
    {
        if (reported == null) return new List<string>();

        var before = SectionHashes(stored);
        return before.Keys.Union(reported.Keys)
            .Where(key => !before.TryGetValue(key, out var a) || !reported.TryGetValue(key, out var b) || a != b)
            .OrderBy(key => key)
            .ToList();
    }

    private static CharacterData RoundTrip(CharacterData data)
        => JsonSerializer.Deserialize<CharacterData>(JsonSerializer.Serialize(data, HashOptions), HashOptions)!;

    private static string ShortHash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
}