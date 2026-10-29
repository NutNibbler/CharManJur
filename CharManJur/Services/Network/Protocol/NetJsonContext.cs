using System.Text.Json.Serialization;
using CharManJur.Models;

namespace CharManJur.Services;

// Source-generated JSON for everything that travels on the wire. No reflection, so it is safe in trimmed /
// packaged builds (the packaged-build crash lesson). Add EVERY new payload type here.
[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Envelope))]
[JsonSerializable(typeof(HelloMessage))]
[JsonSerializable(typeof(AcceptMessage))]
[JsonSerializable(typeof(RejectMessage))]
[JsonSerializable(typeof(PingMessage))]
[JsonSerializable(typeof(NoticeMessage))]
[JsonSerializable(typeof(AckMessage))]
[JsonSerializable(typeof(RosterEntry))]
[JsonSerializable(typeof(RosterMessage))]
[JsonSerializable(typeof(CharacterSnapshotMessage))]
[JsonSerializable(typeof(SnapshotRequestMessage))]
[JsonSerializable(typeof(SummaryChip))]
[JsonSerializable(typeof(CharacterSummaryMessage))]
[JsonSerializable(typeof(GmActionMessage))]
internal sealed partial class NetJsonContext : JsonSerializerContext
{
}