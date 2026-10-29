using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CharManJur.Services;

// The unit that travels in one frame. Payload stays raw JSON until a receiver asks for a concrete type.
public sealed class Envelope
{
    public int V { get; set; } = NetProtocol.Version;
    public string Type { get; set; } = string.Empty;
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? Corr { get; set; }           // set on replies: the Id of the request being answered
    public long Seq { get; set; }             // per-sender counter, for gap detection / resume
    public Guid From { get; set; }
    public JsonElement? Payload { get; set; }
}

public static class EnvelopeCodec
{
    public static byte[] Encode(Envelope envelope)
        => JsonSerializer.SerializeToUtf8Bytes(envelope, NetJsonContext.Default.Envelope);

    // Null when the bytes are not a valid envelope (never throws on bad input).
    public static Envelope? Decode(ReadOnlySpan<byte> data)
    {
        try { return JsonSerializer.Deserialize(data, NetJsonContext.Default.Envelope); }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    public static Envelope Create<T>(string type, T payload, Guid? corr = null)
        => new()
        {
            Type = type,
            Corr = corr,
            Payload = JsonSerializer.SerializeToElement(payload, TypeInfo<T>())
        };

    // Default (null) when the payload is missing or does not match T.
    public static T? ReadPayload<T>(Envelope envelope)
    {
        if (envelope.Payload is not JsonElement payload) return default;
        try { return payload.Deserialize(TypeInfo<T>()); }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { return default; }
    }

    private static JsonTypeInfo<T> TypeInfo<T>()
        => NetJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
           ?? throw new InvalidOperationException($"{typeof(T).Name} is not registered in NetJsonContext.");
}