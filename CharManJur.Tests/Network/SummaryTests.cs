using System.Net;
using System.Net.Sockets;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// Live stat chips: a network player pushes them, the GM keeps the latest for each player.
public class SummaryTests
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

    private static CharacterSummaryMessage Summary(Guid characterGuid, string hp = "12/20")
        => new()
        {
            CharacterGuid = characterGuid,
            Compact = $"HP {hp}",
            Waiting = 1,
            Chips = new List<SummaryChip>
            {
                new() { Label = "HP", Value = hp, Level = 1 },
                new() { Label = "VIG", Value = "8/10", Level = 1 }
            }
        };

    [Fact]
    public async Task Chips_Reach_The_GM_And_The_Latest_Ones_Are_Kept()
    {
        var (hub, port) = await Running();
        try
        {
            int heard = 0;
            hub.OnCharacterSummary = (_, _) => Interlocked.Increment(ref heard);

            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);
            Assert.Null(hub.SummaryFor(hello.PlayerId));

            await client.SendAsync(NetMessageTypes.CharacterSummary, Summary(hello.CharacterGuid, "12/20"));
            await T.WaitUntilAsync(() => hub.SummaryFor(hello.PlayerId)?.Compact == "HP 12/20", "first chips");

            await client.SendAsync(NetMessageTypes.CharacterSummary, Summary(hello.CharacterGuid, "5/20"));
            await T.WaitUntilAsync(() => hub.SummaryFor(hello.PlayerId)?.Compact == "HP 5/20", "newer chips replace older ones");

            var latest = hub.SummaryFor(hello.PlayerId)!;
            Assert.Equal(2, latest.Chips.Count);
            Assert.Equal("5/20", latest.Chips[0].Value);
            Assert.Equal(1, latest.Waiting);
            Assert.True(Volatile.Read(ref heard) >= 2);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Chips_For_Someone_Elses_Character_Are_Ignored_And_Oversized_Ones_Are_Trimmed()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            await client.SendAsync(NetMessageTypes.CharacterSummary, Summary(Guid.NewGuid()));   // not their character

            var big = Summary(hello.CharacterGuid);
            big.Compact = new string('c', 5000);
            big.Waiting = 123456;
            big.Chips = Enumerable.Range(0, 100).Select(i => new SummaryChip { Label = new string('L', 500), Value = new string('V', 500), Level = 99 }).ToList();
            await client.SendAsync(NetMessageTypes.CharacterSummary, big);

            await T.WaitUntilAsync(() => hub.SummaryFor(hello.PlayerId) != null, "the valid chips");
            var kept = hub.SummaryFor(hello.PlayerId)!;
            Assert.Equal(hello.CharacterGuid, kept.CharacterGuid);
            Assert.Equal(200, kept.Compact.Length);
            Assert.Equal(999, kept.Waiting);
            Assert.Equal(16, kept.Chips.Count);
            Assert.Equal(24, kept.Chips[0].Label.Length);
            Assert.Equal(64, kept.Chips[0].Value.Length);
            Assert.Equal(2, kept.Chips[0].Level);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Chips_Are_Forgotten_When_The_Player_Leaves()
    {
        var (hub, port) = await Running();
        try
        {
            var hello = T.Hello();
            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);
            await client.SendAsync(NetMessageTypes.CharacterSummary, Summary(hello.CharacterGuid));
            await T.WaitUntilAsync(() => hub.SummaryFor(hello.PlayerId) != null, "chips");

            await client.LeaveAsync();
            await T.WaitUntilAsync(() => hub.SummaryFor(hello.PlayerId) == null, "chips forgotten");
        }
        finally { await hub.StopAsync(); }
    }
}