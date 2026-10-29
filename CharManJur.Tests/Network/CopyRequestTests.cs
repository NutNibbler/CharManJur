using System.Net;
using System.Net.Sockets;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// Asking one player for a fresh copy (the starting point for a GM bestow). The copy is not stored.
public class CopyRequestTests
{
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

    [Fact]
    public async Task The_Players_Copy_Comes_Back_And_Is_Not_Stored()
    {
        var (hub, port) = await Running();
        try
        {
            int stored = 0;
            hub.OnCharacterSnapshot = (_, _) => { Interlocked.Increment(ref stored); return Task.FromResult(true); };

            var hello = T.Hello();
            await using var client = NewClient();
            string? reasonSeen = null;
            client.MessageReceived += envelope =>
            {
                if (envelope.Type != NetMessageTypes.SnapshotRequest) return;
                reasonSeen = EnvelopeCodec.ReadPayload<SnapshotRequestMessage>(envelope)?.Reason;
                _ = client.ReplyAsync(envelope, NetMessageTypes.CharacterSnapshot, new CharacterSnapshotMessage
                {
                    CharacterGuid = hello.CharacterGuid,
                    Reason = "bestow",
                    GzipJson = SnapshotCodec.Pack("{\"hello\":1}")
                });
            };
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var result = await hub.RequestCopyAsync(hello.PlayerId, "bestow", TimeSpan.FromSeconds(5));

            Assert.True(result.Received);
            Assert.Equal("{\"hello\":1}", SnapshotCodec.Unpack(result.Snapshot!.GzipJson));
            Assert.Equal("bestow", reasonSeen);
            await Task.Delay(200);
            Assert.Equal(0, Volatile.Read(ref stored));   // asking is not the same as storing
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task A_Copy_Of_A_Different_Character_Is_Refused()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            client.MessageReceived += envelope =>
            {
                if (envelope.Type == NetMessageTypes.SnapshotRequest)
                    _ = client.ReplyAsync(envelope, NetMessageTypes.CharacterSnapshot, new CharacterSnapshotMessage
                    {
                        CharacterGuid = Guid.NewGuid(),
                        GzipJson = SnapshotCodec.Pack("{}")
                    });
            };
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var result = await hub.RequestCopyAsync(hello.PlayerId, "bestow", TimeSpan.FromSeconds(5));

            Assert.False(result.Received);
            Assert.Null(result.Snapshot);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Silence_And_Unknown_Players_Are_Reported_Not_Hung_On()
    {
        var (hub, port) = await Running();
        try
        {
            var missing = await hub.RequestCopyAsync(Guid.NewGuid(), "bestow", TimeSpan.FromSeconds(1));
            Assert.False(missing.Received);

            var hello = T.Hello();
            await using var client = NewClient();   // never answers
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var silent = await hub.RequestCopyAsync(hello.PlayerId, "bestow", TimeSpan.FromMilliseconds(400));
            Assert.False(silent.Received);
            Assert.Contains("No answer", silent.Message);
        }
        finally { await hub.StopAsync(); }
    }
}