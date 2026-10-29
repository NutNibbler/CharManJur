using System.Net;
using System.Net.Sockets;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// The GM hub opened to the real network: TCP players next to the in-app ones, the invite code, and the announcement.
public class HubNetworkTests
{
    private static DiscoveryOptions Discovery() => new()
    {
        Port = FreeUdpPort(),
        Interval = TimeSpan.FromMilliseconds(50),
        Expiry = TimeSpan.FromMilliseconds(400),
        Targets = new[] { IPAddress.Loopback }
    };

    private static int FreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

    private static async Task<(GmSessionHub Hub, DiscoveryOptions Discovery, NetworkHostingInfo Info)> Running(SessionCredentials? credentials = null, bool open = true)
    {
        var discovery = Discovery();
        var hub = new GmSessionHub(discovery, "127.0.0.1");
        await hub.StartAsync(Guid.NewGuid(), "Friday Night", credentials);
        hub.SetOpen(open);
        var info = await hub.StartNetworkAsync(0);
        return (hub, discovery, info);
    }

    private static SessionClient TcpClient() => new(new TcpTransport(), T.FastClient());

    [Fact]
    public async Task Network_Info_Is_Consistent()
    {
        var (hub, _, info) = await Running();
        try
        {
            Assert.True(info.Port > 0);
            Assert.True(info.Addresses.Count >= 1);
            Assert.True(info.Addresses[0].EndsWith(":" + info.Port));
            Assert.False(info.PasswordRequired);
            Assert.True(info.Announcing);
            Assert.Equal(info, hub.Network);

            var decoded = InviteCode.TryDecode(info.InviteCode);
            Assert.NotNull(decoded);
            Assert.Equal(info.Addresses[0], decoded!.HostAddress);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Tcp_Player_Joins_With_Password_And_Sits_Next_To_A_Test_Player()
    {
        var (hub, _, info) = await Running(SessionCredentials.FromPassword("dragons", iterations: SessionCredentials.MinIterations));
        try
        {
            Assert.True(info.PasswordRequired);
            Assert.True(InviteCode.TryDecode(info.InviteCode)!.PasswordRequired);

            // A player on the real network (through the loopback address so the test works anywhere)
            await using var tcp = TcpClient();
            var joined = await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello("Real", "Realia"), "dragons");
            Assert.True(joined.Accepted);

            // A test player inside the app, same password rules
            Assert.True((await hub.JoinLocalPlayerAsync(T.Hello("Local", "Locus"), "dragons")).Accepted);

            await T.WaitUntilAsync(() => hub.HostPlayers.Count == 2, "both players on the roster");
            await T.WaitUntilAsync(() => tcp.Roster?.Players.Count == 2, "the roster to reach the real player");
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Test_Players_Inside_The_App_Need_No_Password_But_Network_Players_Do()
    {
        var (hub, _, info) = await Running(SessionCredentials.FromPassword("dragons", iterations: SessionCredentials.MinIterations));
        try
        {
            // The GM's own app already holds the password's key, so its test players join without typing it
            Assert.True((await hub.JoinLocalPlayerAsync(T.Hello("Local", "Locus"))).Accepted);

            // A wrong typed password is still wrong, even for an in-app player
            var wrong = await hub.JoinLocalPlayerAsync(T.Hello("Local2", "Locus2"), "nope");
            Assert.Equal(CharManJur.Models.JoinRejectReason.WrongPassword, wrong.Reason);

            // And nothing changes for a player on the network
            await using var tcp = TcpClient();
            var noPassword = await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello());
            Assert.Equal(CharManJur.Models.JoinRejectReason.WrongPassword, noPassword.Reason);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Tcp_Wrong_Password_Is_Refused()
    {
        var (hub, _, info) = await Running(SessionCredentials.FromPassword("dragons", iterations: SessionCredentials.MinIterations));
        try
        {
            await using var tcp = TcpClient();
            var result = await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello(), "wrong");
            Assert.False(result.Accepted);
            Assert.Equal(CharManJur.Models.JoinRejectReason.WrongPassword, result.Reason);
            Assert.Equal(0, hub.HostPlayers.Count);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Closed_Session_Refuses_Tcp_Players_But_Announces_It()
    {
        var (hub, discovery, info) = await Running(open: false);
        try
        {
            await using var finder = new SessionFinder(discovery);
            finder.Start();
            await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "announcement");
            Assert.False(finder.Sessions[0].Announcement.IsOpen);

            await using var tcp = TcpClient();
            var result = await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello());
            Assert.Equal(CharManJur.Models.JoinRejectReason.Closed, result.Reason);

            hub.SetOpen(true);
            await T.WaitUntilAsync(() => finder.Sessions.Count == 1 && finder.Sessions[0].Announcement.IsOpen, "the open flag to update");
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Announcement_Carries_Name_Password_Flag_And_Player_Count()
    {
        var (hub, discovery, info) = await Running(SessionCredentials.FromPassword("x", iterations: SessionCredentials.MinIterations));
        try
        {
            await using var finder = new SessionFinder(discovery);
            finder.Start();
            await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "announcement");

            var found = finder.Sessions[0];
            Assert.Equal("Friday Night", found.Announcement.SessionName);
            Assert.True(found.Announcement.PasswordRequired);
            Assert.Equal(info.Port, found.Announcement.TcpPort);
            Assert.Equal(0, found.Announcement.PlayerCount);

            await using var tcp = TcpClient();
            Assert.True((await tcp.JoinAsync(found.Address, T.Hello(), "x")).Accepted);
            await T.WaitUntilAsync(() => finder.Sessions.Count == 1 && finder.Sessions[0].Announcement.PlayerCount == 1, "player count");
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Stopping_The_Network_Keeps_Connected_Players_But_Refuses_New_Ones()
    {
        var (hub, discovery, info) = await Running();
        try
        {
            await using var first = TcpClient();
            Assert.True((await first.JoinAsync($"127.0.0.1:{info.Port}", T.Hello("A", "Alpha"))).Accepted);

            await hub.StopNetworkAsync();
            Assert.Null(hub.Network);
            Assert.True(hub.IsRunning);

            await using var second = TcpClient();
            var late = await second.JoinAsync($"127.0.0.1:{info.Port}", T.Hello("B", "Beta"));
            Assert.False(late.Accepted);   // nobody is listening any more

            Assert.Equal(ClientState.Connected, first.State);
            Assert.Equal(1, hub.HostPlayers.Count);

            await using var finder = new SessionFinder(discovery);
            finder.Start();
            await Task.Delay(300);
            Assert.Equal(0, finder.Sessions.Count);   // and it is no longer announced
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Stopping_The_Session_Closes_The_Network_And_Tells_Tcp_Players()
    {
        var (hub, _, info) = await Running();
        await using var tcp = TcpClient();
        Assert.True((await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello())).Accepted);

        await hub.StopAsync();
        Assert.Null(hub.Network);
        await T.WaitUntilAsync(() => tcp.State == ClientState.SessionClosed, "the player to be told");
    }

    [Fact]
    public async Task Network_Cannot_Start_Without_A_Session_And_Starting_Twice_Is_Harmless()
    {
        var hub = new GmSessionHub(Discovery(), "127.0.0.1");
        Assert.True(await ThrowsInvalid(() => hub.StartNetworkAsync(0)));

        await hub.StartAsync(Guid.NewGuid(), "S");
        var first = await hub.StartNetworkAsync(0);
        var second = await hub.StartNetworkAsync(0);
        Assert.Equal(first, second);
        await hub.StopAsync();
    }

    private static async Task<bool> ThrowsInvalid(Func<Task> action)
    {
        try { await action(); return false; }
        catch (InvalidOperationException) { return true; }
    }

    [Fact]
    public async Task Busy_Port_Falls_Back_To_Another()
    {
        using var squatter = new TcpListener(IPAddress.Loopback, 0);
        squatter.Start();
        int busy = ((IPEndPoint)squatter.LocalEndpoint).Port;

        var hub = new GmSessionHub(Discovery(), "127.0.0.1");
        try
        {
            await hub.StartAsync(Guid.NewGuid(), "S");
            hub.SetOpen(true);
            var info = await hub.StartNetworkAsync(busy);
            Assert.NotEqual(busy, info.Port);

            await using var tcp = TcpClient();
            Assert.True((await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello())).Accepted);
        }
        finally { await hub.StopAsync(); }
    }

    [Fact]
    public async Task Restarting_The_Session_Needs_The_Network_Started_Again()
    {
        var (hub, _, _) = await Running();
        try
        {
            await hub.StartAsync(Guid.NewGuid(), "Second");   // replaces the session
            Assert.Null(hub.Network);

            hub.SetOpen(true);
            var info = await hub.StartNetworkAsync(0);
            await using var tcp = TcpClient();
            Assert.True((await tcp.JoinAsync($"127.0.0.1:{info.Port}", T.Hello())).Accepted);
        }
        finally { await hub.StopAsync(); }
    }
}