using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

// All hub tests are in one class so they run one after another (the hub owns the static NetLog sink).
public class GmSessionHubTests
{
    private static async Task<GmSessionHub> Running(bool open = true)
    {
        var hub = new GmSessionHub();
        await hub.StartAsync(Guid.NewGuid(), "Hub Test");
        hub.SetOpen(open);
        return hub;
    }

    [Fact]
    public async Task Start_Join_And_Stop()
    {
        var hub = await Running();
        Assert.True(hub.IsRunning);
        Assert.True(hub.IsOpen);

        var hello = T.Hello("Ann", "Aria");
        var result = await hub.JoinLocalPlayerAsync(hello);
        Assert.True(result.Accepted);
        Assert.Equal(1, hub.HostPlayers.Count);
        Assert.Equal(1, hub.LocalPlayers.Count);
        Assert.Equal(ClientState.Connected, hub.LocalPlayers[0].State);

        await hub.StopAsync();
        Assert.False(hub.IsRunning);
        Assert.False(hub.IsOpen);
        Assert.Equal(0, hub.HostPlayers.Count);
        Assert.Equal(0, hub.LocalPlayers.Count);
    }

    [Fact]
    public async Task Join_Is_Refused_When_Not_Running_Or_Closed()
    {
        var hub = new GmSessionHub();
        Assert.False((await hub.JoinLocalPlayerAsync(T.Hello())).Accepted);

        await hub.StartAsync(Guid.NewGuid(), "Closed");   // starts closed
        var closed = await hub.JoinLocalPlayerAsync(T.Hello());
        Assert.False(closed.Accepted);
        Assert.Equal(JoinRejectReason.Closed, closed.Reason);
        Assert.Equal(0, hub.LocalPlayers.Count);
        await hub.StopAsync();
    }

    [Fact]
    public async Task Kick_Removes_Player_And_Raises_Left()
    {
        var hub = await Running();
        var left = new List<PlayerLeaveReason>();
        hub.OnPlayerLeft = (_, reason) => { lock (left) left.Add(reason); };

        var hello = T.Hello();
        await hub.JoinLocalPlayerAsync(hello);
        Assert.True(await hub.KickAsync(hello.PlayerId));

        await T.WaitUntilAsync(() => hub.HostPlayers.Count == 0 && hub.LocalPlayers.Count == 0, "player removed on both sides");
        lock (left) Assert.Equal(PlayerLeaveReason.Kicked, left[0]);
        Assert.False(await hub.KickAsync(hello.PlayerId));   // already gone
        await hub.StopAsync();
    }

    [Fact]
    public async Task Leave_Raises_Left()
    {
        var hub = await Running();
        var left = new List<PlayerLeaveReason>();
        hub.OnPlayerLeft = (_, reason) => { lock (left) left.Add(reason); };

        var hello = T.Hello();
        await hub.JoinLocalPlayerAsync(hello);
        await hub.LeaveLocalPlayerAsync(hello.PlayerId);

        await T.WaitUntilAsync(() => { lock (left) return left.Count == 1; }, "left event");
        lock (left) Assert.Equal(PlayerLeaveReason.Left, left[0]);
        Assert.Equal(0, hub.LocalPlayers.Count);
        await hub.StopAsync();
    }

    [Fact]
    public async Task Stop_Tells_Players_The_Session_Closed()
    {
        var hub = await Running();
        var left = new List<PlayerLeaveReason>();
        hub.OnPlayerLeft = (_, reason) => { lock (left) left.Add(reason); };
        await hub.JoinLocalPlayerAsync(T.Hello("A", "CA"));
        await hub.JoinLocalPlayerAsync(T.Hello("B", "CB"));

        await hub.StopAsync();
        lock (left)
        {
            Assert.Equal(2, left.Count);
            Assert.True(left.All(r => r == PlayerLeaveReason.SessionClosed));
        }
    }

    [Fact]
    public async Task Dropped_Links_Reconnect_And_Report_It()
    {
        var hub = await Running();
        var states = new List<bool>();
        var joins = new List<bool>();
        hub.OnPlayerStateChanged = p => { lock (states) states.Add(p.IsConnected); };
        hub.OnPlayerJoined = (_, resumed) => { lock (joins) joins.Add(resumed); };

        var hello = T.Hello();
        await hub.JoinLocalPlayerAsync(hello);
        await hub.DropAllLinksAsync();

        await T.WaitUntilAsync(() => { lock (states) return states.Contains(false); }, "disconnect reported");
        await T.WaitUntilAsync(() => hub.HostPlayers.Count == 1 && hub.HostPlayers[0].IsConnected, "player back", 10000);
        lock (joins) Assert.Equal(true, joins[^1]);   // the second join was a resume
        Assert.Equal(1, hub.HostPlayers.Count);       // same seat, not a second player
        await hub.StopAsync();
    }

    [Fact]
    public async Task Starting_Again_Replaces_The_Session()
    {
        var hub = await Running();
        await hub.JoinLocalPlayerAsync(T.Hello());

        var other = Guid.NewGuid();
        await hub.StartAsync(other, "Second");
        Assert.Equal(other, hub.CampaignId);
        Assert.Equal("Second", hub.SessionName);
        Assert.Equal(0, hub.HostPlayers.Count);
        Assert.Equal(0, hub.LocalPlayers.Count);
        Assert.False(hub.IsOpen);   // every session starts closed
        await hub.StopAsync();
    }

    [Fact]
    public async Task Lab_Session_Is_Flagged_And_Trace_Is_Kept()
    {
        var hub = new GmSessionHub();
        var seen = new List<string>();
        hub.TraceAdded += line => { lock (seen) seen.Add(line); };

        await hub.StartAsync(Guid.Empty, "Lab");
        Assert.True(hub.IsLabSession);
        NetLog.Info("marker-line");

        Assert.True(hub.TraceLines.Any(l => l.Contains("marker-line")));
        lock (seen) Assert.True(seen.Any(l => l.Contains("marker-line")));
        await hub.StopAsync();
        Assert.False(hub.IsLabSession);
    }

    [Fact]
    public async Task Eight_Local_Players_Fit_And_A_Ninth_Is_Refused()
    {
        var hub = await Running();
        for (int i = 0; i < 8; i++)
            Assert.True((await hub.JoinLocalPlayerAsync(T.Hello("P" + i, "C" + i))).Accepted);

        var ninth = await hub.JoinLocalPlayerAsync(T.Hello("P9", "C9"));
        Assert.False(ninth.Accepted);
        Assert.Equal(JoinRejectReason.Full, ninth.Reason);
        await hub.StopAsync();
    }
}