using System.Text;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class FrameAndEnvelopeTests
{
    [Fact]
    public async Task Frame_RoundTrips_Several_Frames()
    {
        var stream = new MemoryStream();
        await FrameCodec.WriteFrameAsync(stream, new byte[] { 1, 2, 3 });
        await FrameCodec.WriteFrameAsync(stream, Array.Empty<byte>());
        await FrameCodec.WriteFrameAsync(stream, new byte[] { 9 });
        stream.Position = 0;

        Assert.Equal(3, (await FrameCodec.ReadFrameAsync(stream))!.Length);
        Assert.Equal(0, (await FrameCodec.ReadFrameAsync(stream))!.Length);
        Assert.Equal((byte)9, (await FrameCodec.ReadFrameAsync(stream))![0]);
        Assert.Null(await FrameCodec.ReadFrameAsync(stream));   // clean end
    }

    [Fact]
    public async Task Frame_Truncated_Throws()
    {
        var stream = new MemoryStream();
        await FrameCodec.WriteFrameAsync(stream, new byte[] { 1, 2, 3, 4, 5 });
        var cut = new MemoryStream(stream.ToArray().Take(6).ToArray());
        await Assert.ThrowsAnyAsync<EndOfStreamException>(() => FrameCodec.ReadFrameAsync(cut));
    }

    [Fact]
    public async Task Frame_Oversized_Is_Refused_Both_Ways()
    {
        await Assert.ThrowsAnyAsync<InvalidDataException>(
            () => FrameCodec.WriteFrameAsync(new MemoryStream(), new byte[FrameCodec.MaxFrameBytes + 1]));

        var evil = new MemoryStream(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 1 });   // claims 2 GB
        await Assert.ThrowsAnyAsync<InvalidDataException>(() => FrameCodec.ReadFrameAsync(evil));
    }

    [Fact]
    public void Envelope_RoundTrips_A_Payload()
    {
        var hello = T.Hello("Ann", "Aria");
        var envelope = EnvelopeCodec.Create(NetMessageTypes.Hello, hello);
        envelope.Seq = 7;

        var decoded = EnvelopeCodec.Decode(EnvelopeCodec.Encode(envelope));
        Assert.NotNull(decoded);
        Assert.Equal(NetMessageTypes.Hello, decoded!.Type);
        Assert.Equal(7L, decoded.Seq);
        Assert.Equal(envelope.Id, decoded.Id);

        var back = EnvelopeCodec.ReadPayload<HelloMessage>(decoded);
        Assert.NotNull(back);
        Assert.Equal(hello.PlayerId, back!.PlayerId);
        Assert.Equal("Aria", back.CharacterName);
        Assert.Equal(NetProtocol.Version, back.ProtocolVersion);
    }

    [Fact]
    public void Enums_Travel_As_Text()
    {
        var envelope = EnvelopeCodec.Create(NetMessageTypes.Reject,
            new RejectMessage { Reason = JoinRejectReason.VersionMismatch, Message = "x" });
        string json = Encoding.UTF8.GetString(EnvelopeCodec.Encode(envelope));
        Assert.Contains("VersionMismatch", json);
        Assert.Equal(JoinRejectReason.VersionMismatch, EnvelopeCodec.ReadPayload<RejectMessage>(EnvelopeCodec.Decode(Encoding.UTF8.GetBytes(json))!)!.Reason);
    }

    [Fact]
    public void Garbage_Decodes_To_Null()
    {
        Assert.Null(EnvelopeCodec.Decode(Encoding.UTF8.GetBytes("not json at all")));
        Assert.Null(EnvelopeCodec.Decode(new byte[] { 0, 1, 2, 3 }));
        Assert.Null(EnvelopeCodec.Decode(Array.Empty<byte>()));
    }

    [Fact]
    public void Wrong_Payload_Type_Reads_As_Null_Not_A_Crash()
    {
        var envelope = EnvelopeCodec.Create(NetMessageTypes.Ping, new PingMessage { Ticks = 5 });
        envelope.Payload = System.Text.Json.JsonDocument.Parse("\"just a string\"").RootElement;
        Assert.Null(EnvelopeCodec.ReadPayload<RosterMessage>(envelope));
    }

    private sealed class NotRegistered { public int X { get; set; } }

    [Fact]
    public void Unregistered_Payload_Types_Fail_Loudly()
    {
        var ex = Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
        {
            EnvelopeCodec.Create("x", new NotRegistered());
            return Task.CompletedTask;
        });
        ex.GetAwaiter().GetResult();
    }

    [Fact]
    public void Every_Payload_Type_Is_Registered()
    {
        // If you add a payload class, add it to NetJsonContext; this guards the packaged/trimmed build.
        foreach (var payload in new object[]
        {
            new HelloMessage(), new AcceptMessage(), new RejectMessage(), new PingMessage(),
            new NoticeMessage(), new AckMessage(), new RosterEntry(), new RosterMessage()
        })
        {
            var create = typeof(EnvelopeCodec).GetMethod(nameof(EnvelopeCodec.Create))!.MakeGenericMethod(payload.GetType());
            var envelope = (Envelope)create.Invoke(null, new object?[] { "t", payload, null })!;
            Assert.NotNull(EnvelopeCodec.Decode(EnvelopeCodec.Encode(envelope)));
        }
    }
}
