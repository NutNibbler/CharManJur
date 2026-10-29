using System.Net;
using System.Net.Sockets;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// GM actions: sent to a network player, confirmed by their app, safe to repeat.
public class GmActionTests
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

    private static GmActionMessage Action(string kind = "damage") => new() { ActionId = Guid.NewGuid(), Kind = kind, Json = "{\"Amount\":7}" };

    [Fact]
    public async Task A_Confirmed_Action_Reaches_The_Player_And_Reports_Delivered()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            GmActionMessage? received = null;
            client.MessageReceived += envelope =>
            {
                if (envelope.Type != NetMessageTypes.GmAction) return;
                received = EnvelopeCodec.ReadPayload<GmActionMessage>(envelope);
                _ = client.ReplyAsync(envelope, NetMessageTypes.Ack, new AckMessage { Ok = true, Message = "Queued." });
            };
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var action = Action();
            var result = await hub.SendActionAsync(hello.PlayerId, action, TimeSpan.FromSeconds(5));

            Assert.True(result.Delivered);
            Assert.Equal("Queued.", result.Message);
            Assert.NotNull(received);
            Assert.Equal(action.ActionId, received!.ActionId);
            Assert.Equal("damage", received.Kind);
            Assert.Equal(action.Json, received.Json);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task A_Refusal_From_The_Player_Is_Reported_With_Its_Reason()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            client.MessageReceived += envelope =>
            {
                if (envelope.Type == NetMessageTypes.GmAction)
                    _ = client.ReplyAsync(envelope, NetMessageTypes.Ack, new AckMessage { Ok = false, Message = "Unknown action." });
            };
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var result = await hub.SendActionAsync(hello.PlayerId, Action("mystery"), TimeSpan.FromSeconds(5));

            Assert.False(result.Delivered);
            Assert.Equal("Unknown action.", result.Message);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task No_Answer_Reports_Not_Confirmed_Instead_Of_Hanging()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();   // never replies
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            var result = await hub.SendActionAsync(hello.PlayerId, Action(), TimeSpan.FromMilliseconds(400));

            Assert.False(result.Delivered);
            Assert.Contains("No confirmation", result.Message);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Unknown_And_Disconnected_Players_Are_Refused_Without_Sending()
    {
        var (hub, port) = await Running();
        try
        {
            var unknown = await hub.SendActionAsync(Guid.NewGuid(), Action(), TimeSpan.FromSeconds(1));
            Assert.False(unknown.Delivered);
            Assert.Contains("not in the session", unknown.Message);

            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);
            await hub.DropAllLinksAsync();
            await T.WaitUntilAsync(() => hub.HostPlayers.Any(p => !p.IsConnected), "the link to drop");

            var down = await hub.SendActionAsync(hello.PlayerId, Action(), TimeSpan.FromSeconds(1));
            Assert.False(down.Delivered);
            Assert.Contains("connection is lost", down.Message);
        }
        finally { await hub.StopAsync(); }
    }
}