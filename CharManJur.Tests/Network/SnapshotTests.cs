using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// Character copies: packing for the wire, and the hub taking them from players (on leaving, and when the GM asks).
public class SnapshotTests
{
    // ---------------------------------------------------------------- the codec

    [Fact]
    public void Codec_Round_Trips_Text_And_Shrinks_It()
    {
        string json = "{\"name\":\"" + new string('a', 50_000) + "\",\"emoji\":\"🧙 ünïcode\"}";
        byte[] packed = SnapshotCodec.Pack(json);
        Assert.True(packed.Length < json.Length / 10);
        Assert.Equal(json, SnapshotCodec.Unpack(packed));
    }

    [Fact]
    public void Codec_Refuses_Garbage_Empty_And_Oversized_Input()
    {
        Assert.Throws<InvalidDataException>(() => SnapshotCodec.Unpack(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.Throws<InvalidDataException>(() => SnapshotCodec.Unpack(Array.Empty<byte>()));
        Assert.Throws<InvalidDataException>(() => SnapshotCodec.Unpack(new byte[SnapshotCodec.MaxPackedBytes + 1]));
    }

    [Fact]
    public void Codec_Refuses_A_Small_File_That_Expands_Hugely()
    {
        // 40 MB of zeros gzips to a few tens of KB: it must not be inflated.
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            var zeros = new byte[1024 * 1024];
            for (int i = 0; i < 40; i++) gzip.Write(zeros, 0, zeros.Length);
        }

        byte[] bomb = output.ToArray();
        Assert.True(bomb.Length < SnapshotCodec.MaxPackedBytes);
        Assert.Throws<InvalidDataException>(() => SnapshotCodec.Unpack(bomb));
    }

    [Fact]
    public void Messages_Survive_The_Envelope()
    {
        var message = new CharacterSnapshotMessage
        {
            CharacterGuid = Guid.NewGuid(),
            Reason = "leaving",
            GzipJson = SnapshotCodec.Pack("{\"x\":1}")
        };

        var back = EnvelopeCodec.ReadPayload<CharacterSnapshotMessage>(
            EnvelopeCodec.Decode(EnvelopeCodec.Encode(EnvelopeCodec.Create(NetMessageTypes.CharacterSnapshot, message)))!);

        Assert.NotNull(back);
        Assert.Equal(message.CharacterGuid, back!.CharacterGuid);
        Assert.Equal("{\"x\":1}", SnapshotCodec.Unpack(back.GzipJson));
    }

    // ---------------------------------------------------------------- the hub

    private static int FreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

    private static async Task<(GmSessionHub Hub, int Port)> Running()
    {
        var hub = new GmSessionHub(new DiscoveryOptions { Port = FreeUdpPort(), Targets = new[] { IPAddress.Loopback } }, "127.0.0.1");
        await hub.StartAsync(Guid.NewGuid(), "Friday Night");
        hub.SetOpen(true);
        var info = await hub.StartNetworkAsync(0);
        return (hub, info.Port);
    }

    private static SessionClient NewClient() => new(new TcpTransport(), T.FastClient());

    private static CharacterSnapshotMessage CopyFor(Guid characterGuid, string json = "{\"hp\":7}")
        => new() { CharacterGuid = characterGuid, Reason = "leaving", GzipJson = SnapshotCodec.Pack(json) };

    [Fact]
    public async Task Leaving_Player_Sends_A_Copy_And_Is_Told_It_Was_Stored()
    {
        var (hub, port) = await Running();
        try
        {
            CharacterSnapshotMessage? stored = null;
            hub.OnCharacterSnapshot = (player, message) => { stored = message; return Task.FromResult(true); };

            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var reply = await client.RequestAsync(NetMessageTypes.CharacterSnapshot, CopyFor(hello.CharacterGuid), TimeSpan.FromSeconds(5));
            Assert.True(EnvelopeCodec.ReadPayload<AckMessage>(reply)!.Ok);
            Assert.NotNull(stored);
            Assert.Equal("{\"hp\":7}", SnapshotCodec.Unpack(stored!.GzipJson));
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task A_Copy_Of_Someone_Elses_Character_Is_Refused()
    {
        var (hub, port) = await Running();
        try
        {
            bool called = false;
            hub.OnCharacterSnapshot = (_, _) => { called = true; return Task.FromResult(true); };

            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", T.Hello())).Accepted);

            var reply = await client.RequestAsync(NetMessageTypes.CharacterSnapshot, CopyFor(Guid.NewGuid()), TimeSpan.FromSeconds(5));
            var ack = EnvelopeCodec.ReadPayload<AckMessage>(reply)!;
            Assert.False(ack.Ok);
            Assert.False(called);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Not_Stored_Means_Not_Acknowledged()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            // Nobody is listening for copies yet
            var none = await client.RequestAsync(NetMessageTypes.CharacterSnapshot, CopyFor(hello.CharacterGuid), TimeSpan.FromSeconds(5));
            Assert.False(EnvelopeCodec.ReadPayload<AckMessage>(none)!.Ok);

            // The GM's store says no
            hub.OnCharacterSnapshot = (_, _) => Task.FromResult(false);
            var no = await client.RequestAsync(NetMessageTypes.CharacterSnapshot, CopyFor(hello.CharacterGuid), TimeSpan.FromSeconds(5));
            Assert.False(EnvelopeCodec.ReadPayload<AckMessage>(no)!.Ok);

            // The GM's store crashes: still a clean "no", and the session keeps working
            hub.OnCharacterSnapshot = (_, _) => throw new InvalidOperationException("disk full");
            var crashed = await client.RequestAsync(NetMessageTypes.CharacterSnapshot, CopyFor(hello.CharacterGuid), TimeSpan.FromSeconds(5));
            Assert.False(EnvelopeCodec.ReadPayload<AckMessage>(crashed)!.Ok);
            Assert.Equal(ClientState.Connected, client.State);
        }
        finally { await hub.StopAsync(); }
    }

    // A network player that answers the GM's request the way the real app will.
    private static void AnswerRequests(SessionClient client, Guid characterGuid, string json)
    {
        client.MessageReceived += envelope =>
        {
            if (envelope.Type == NetMessageTypes.SnapshotRequest)
                _ = client.ReplyAsync(envelope, NetMessageTypes.CharacterSnapshot, CopyFor(characterGuid, json));
        };
    }

    [Fact]
    public async Task Closing_Time_Collects_A_Copy_From_Every_Network_Player_And_Skips_Test_Players()
    {
        var (hub, port) = await Running();
        try
        {
            var stored = new Dictionary<Guid, string>();
            hub.OnCharacterSnapshot = (player, message) =>
            {
                lock (stored) stored[player.CharacterGuid] = SnapshotCodec.Unpack(message.GzipJson);
                return Task.FromResult(true);
            };

            var helloA = T.Hello("Ann", "Aria");
            var helloB = T.Hello("Bo", "Bran");
            await using var a = NewClient();
            await using var b = NewClient();
            Assert.True((await a.JoinAsync($"127.0.0.1:{port}", helloA)).Accepted);
            Assert.True((await b.JoinAsync($"127.0.0.1:{port}", helloB)).Accepted);
            AnswerRequests(a, helloA.CharacterGuid, "{\"who\":\"aria\"}");
            AnswerRequests(b, helloB.CharacterGuid, "{\"who\":\"bran\"}");

            // A test player inside the app never answers and must not be asked
            Assert.True((await hub.JoinLocalPlayerAsync(T.Hello("Test", "Tess"))).Accepted);
            await T.WaitUntilAsync(() => hub.HostPlayers.Count == 3, "three players");

            var results = await hub.RequestSnapshotsAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.Received, r.Message));
            Assert.Equal("{\"who\":\"aria\"}", stored[helloA.CharacterGuid]);
            Assert.Equal("{\"who\":\"bran\"}", stored[helloB.CharacterGuid]);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task A_Silent_Player_Does_Not_Hold_Up_The_Others()
    {
        var (hub, port) = await Running();
        try
        {
            hub.OnCharacterSnapshot = (_, _) => Task.FromResult(true);

            var helloA = T.Hello("Ann", "Aria");
            var helloB = T.Hello("Bo", "Bran");
            await using var talker = NewClient();
            await using var silent = NewClient();
            Assert.True((await talker.JoinAsync($"127.0.0.1:{port}", helloA)).Accepted);
            Assert.True((await silent.JoinAsync($"127.0.0.1:{port}", helloB)).Accepted);
            AnswerRequests(talker, helloA.CharacterGuid, "{}");   // `silent` never answers

            var started = DateTime.UtcNow;
            var results = await hub.RequestSnapshotsAsync(TimeSpan.FromMilliseconds(600));

            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(3), "players are asked together, not one after another");
            Assert.True(results.Single(r => r.Player.CharacterGuid == helloA.CharacterGuid).Received);
            Assert.False(results.Single(r => r.Player.CharacterGuid == helloB.CharacterGuid).Received);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Asking_With_No_Session_Or_No_Players_Is_Harmless()
    {
        var hub = new GmSessionHub(new DiscoveryOptions { Port = FreeUdpPort(), Targets = new[] { IPAddress.Loopback } }, "127.0.0.1");
        Assert.Empty(await hub.RequestSnapshotsAsync(TimeSpan.FromSeconds(1)));

        await hub.StartAsync(Guid.NewGuid(), "S");
        Assert.Empty(await hub.RequestSnapshotsAsync(TimeSpan.FromSeconds(1)));
        await hub.StopAsync();
    }
}