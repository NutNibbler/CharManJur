using System.Net;
using System.Net.Sockets;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// The hello carries short fingerprints of the player's saved character, so the GM can tell whether it changed since
// the last copy the GM stored (no character data travels in the hello).
public class HelloFingerprintTests
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
    public async Task The_Hello_Carries_Character_Fingerprints_To_The_GM_On_Join_And_On_Return()
    {
        var (hub, port) = await Running();
        try
        {
            var seen = new List<(string? Hash, int Sections, bool Resumed)>();
            hub.OnPlayerJoined = (player, resumed) =>
            {
                lock (seen) seen.Add((player.DataHash, player.SectionHashes?.Count ?? -1, resumed));
            };

            var hello = T.Hello();
            hello.DataHash = "ABC123";
            hello.SectionHashes = new Dictionary<string, string> { ["Inventory"] = "11", ["Favour"] = "22" };

            await using (var first = NewClient())
            {
                Assert.True((await first.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);
                await T.WaitUntilAsync(() => { lock (seen) return seen.Count == 1; }, "first join reported");
                await first.LeaveAsync();
            }
            await T.WaitUntilAsync(() => hub.HostPlayers.Count == 0, "the first visit to end");

            hello.DataHash = "DEF456";   // the character changed while they were away
            await using var second = NewClient();
            Assert.True((await second.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);

            await T.WaitUntilAsync(() => { lock (seen) return seen.Count == 2; }, "both joins reported");
            lock (seen)
            {
                Assert.Equal("ABC123", seen[0].Hash);
                Assert.Equal(2, seen[0].Sections);
                Assert.Equal("DEF456", seen[1].Hash);
            }
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Oversized_Or_Missing_Fingerprints_Are_Dropped_Not_Trusted()
    {
        var (hub, port) = await Running();
        try
        {
            PlayerConnection? joined = null;
            hub.OnPlayerJoined = (player, _) => joined = player;

            var hello = T.Hello();
            hello.DataHash = new string('x', 5000);
            hello.SectionHashes = Enumerable.Range(0, 600).ToDictionary(i => "S" + i, i => "h");

            await using var client = NewClient();
            Assert.True((await client.JoinAsync($"127.0.0.1:{port}", hello)).Accepted);
            await T.WaitUntilAsync(() => joined != null, "join reported");

            Assert.Null(joined!.DataHash);
            Assert.Null(joined.SectionHashes);
        }
        finally { await hub.StopAsync(); }
    }
}