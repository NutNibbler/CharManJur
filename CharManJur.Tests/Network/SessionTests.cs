using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class SessionTests
{
    private sealed class Rig
    {
        public T.Net Net = null!;
        public SessionHost Host = null!;
        public ITransport ClientTransport = null!;
        public string Address = string.Empty;

        public SessionClient NewClient(SessionClientOptions? o = null) => new(ClientTransport, o ?? T.FastClient());
    }

    private static async Task<Rig> Start(bool tcp, bool open = true, int maxPlayers = 8, Func<ITransport, ITransport>? wrapClient = null)
    {
        var net = tcp ? T.Net.Tcp() : T.Net.Loopback();
        var host = new SessionHost(T.FastHost(maxPlayers));
        await host.StartAsync(net.Transport, net.ListenAddress);
        host.SetOpen(open);
        return new Rig
        {
            Net = net,
            Host = host,
            ClientTransport = wrapClient != null ? wrapClient(net.Transport) : net.Transport,
            Address = net.ClientAddress(host)
        };
    }

    [Fact] public Task Join_Loopback() => Join(false);
    [Fact] public Task Join_Tcp() => Join(true);

    private static async Task Join(bool tcp)
    {
        var rig = await Start(tcp);
        await using var host = rig.Host;
        await using var client = rig.NewClient();

        var result = await client.JoinAsync(rig.Address, T.Hello("Ann", "Aria"));
        Assert.True(result.Accepted);
        Assert.Equal(ClientState.Connected, client.State);
        Assert.Equal(1, host.Players.Count);
        Assert.Equal("Aria", host.Players[0].CharacterName);
        await T.WaitUntilAsync(() => client.Roster != null && client.Roster.Players.Count == 1, "roster to arrive");
        Assert.Equal("Test Campaign", client.Roster!.SessionName);
    }

    [Fact]
    public async Task Closed_Session_Refuses_New_Players()
    {
        var rig = await Start(false, open: false);
        await using var host = rig.Host;
        await using var client = rig.NewClient();

        var result = await client.JoinAsync(rig.Address, T.Hello());
        Assert.False(result.Accepted);
        Assert.Equal(JoinRejectReason.Closed, result.Reason);
        Assert.Equal(ClientState.Rejected, client.State);
        Assert.Equal(0, host.Players.Count);
    }

    [Fact]
    public async Task Full_Session_Refuses_Extra_Player()
    {
        var rig = await Start(false, maxPlayers: 2);
        await using var host = rig.Host;
        await using var a = rig.NewClient();
        await using var b = rig.NewClient();
        await using var c = rig.NewClient();

        Assert.True((await a.JoinAsync(rig.Address, T.Hello("A", "CA"))).Accepted);
        Assert.True((await b.JoinAsync(rig.Address, T.Hello("B", "CB"))).Accepted);
        var third = await c.JoinAsync(rig.Address, T.Hello("C", "CC"));
        Assert.False(third.Accepted);
        Assert.Equal(JoinRejectReason.Full, third.Reason);
    }

    [Fact]
    public async Task Same_Character_Cannot_Join_Twice()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        await using var a = rig.NewClient();
        await using var b = rig.NewClient();
        var character = Guid.NewGuid();

        Assert.True((await a.JoinAsync(rig.Address, T.Hello("A", "Aria", characterGuid: character))).Accepted);
        var second = await b.JoinAsync(rig.Address, T.Hello("B", "Aria", characterGuid: character));
        Assert.False(second.Accepted);
        Assert.Equal(JoinRejectReason.DuplicateCharacter, second.Reason);
    }

    [Fact]
    public async Task Protocol_Mismatch_Is_Refused()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        var hello = T.Hello();
        hello.ProtocolVersion = NetProtocol.Version + 99;

        var result = await client.JoinAsync(rig.Address, hello);
        Assert.False(result.Accepted);
        Assert.Equal(JoinRejectReason.VersionMismatch, result.Reason);
    }

    [Fact]
    public async Task Join_Validator_Can_Refuse()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        host.JoinValidator = h => Task.FromResult<JoinRejection?>(
            h.CharacterName == "Banned" ? new JoinRejection(JoinRejectReason.PolicyRefused, "nope") : null);
        await using var a = rig.NewClient();
        await using var b = rig.NewClient();

        var bad = await a.JoinAsync(rig.Address, T.Hello("A", "Banned"));
        Assert.False(bad.Accepted);
        Assert.Equal(JoinRejectReason.PolicyRefused, bad.Reason);
        Assert.True((await b.JoinAsync(rig.Address, T.Hello("B", "Fine"))).Accepted);
    }

    [Fact]
    public async Task Nobody_Listening_Fails_Cleanly()
    {
        var client = new SessionClient(new LoopbackTransport(), T.FastClient());
        var result = await client.JoinAsync("nowhere", T.Hello());
        Assert.False(result.Accepted);
        Assert.NotEqual(ClientState.Connected, client.State);
        await client.DisposeAsync();
    }

    [Fact]
    public async Task Kick_Removes_And_Never_Reconnects()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        var hello = T.Hello();
        await client.JoinAsync(rig.Address, hello);

        Assert.True(await host.KickAsync(hello.PlayerId, "bye"));
        await T.WaitUntilAsync(() => client.State == ClientState.Kicked, "client to be kicked");
        Assert.Equal("bye", client.LastMessage);
        Assert.Equal(0, host.Players.Count);

        await Task.Delay(400);   // several reconnect delays; it must stay kicked
        Assert.Equal(ClientState.Kicked, client.State);
    }

    [Fact]
    public async Task Kicked_Player_Cannot_Rejoin_Until_Allowed()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        var hello = T.Hello();
        await using (var first = rig.NewClient())
        {
            await first.JoinAsync(rig.Address, hello);
            await host.KickAsync(hello.PlayerId);
            await T.WaitUntilAsync(() => first.State == ClientState.Kicked, "kicked");
        }

        await using var again = rig.NewClient();
        var refused = await again.JoinAsync(rig.Address, hello);
        Assert.False(refused.Accepted);
        Assert.Equal(JoinRejectReason.Kicked, refused.Reason);

        host.AllowRejoin(hello.PlayerId);
        await using var third = rig.NewClient();
        Assert.True((await third.JoinAsync(rig.Address, hello)).Accepted);
    }

    [Fact]
    public async Task Leave_Removes_The_Player()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        await client.JoinAsync(rig.Address, T.Hello());

        await client.LeaveAsync();
        Assert.Equal(ClientState.Left, client.State);
        await T.WaitUntilAsync(() => host.Players.Count == 0, "host to drop the player");
    }

    [Fact]
    public async Task Host_Stop_Tells_Players_The_Session_Closed()
    {
        var rig = await Start(false);
        await using var client = rig.NewClient();
        await client.JoinAsync(rig.Address, T.Hello());

        await rig.Host.StopAsync();
        await T.WaitUntilAsync(() => client.State == ClientState.SessionClosed, "session closed state");
    }

    [Fact]
    public async Task Dropped_Link_Reconnects_And_Resumes_Same_Seat()
    {
        var chaos = (ChaosTransport?)null;
        var rig = await Start(false, wrapClient: t => chaos = new ChaosTransport(t));
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        var hello = T.Hello();
        var resumedFlags = new List<bool>();
        host.PlayerJoined += (_, resumed) => { lock (resumedFlags) resumedFlags.Add(resumed); };
        var states = new List<ClientState>();
        client.StateChanged += s => { lock (states) states.Add(s); };

        await client.JoinAsync(rig.Address, hello);
        await chaos!.DropAllConnectionsAsync();

        await T.WaitUntilAsync(() => { lock (states) return states.Contains(ClientState.Reconnecting); }, "reconnecting state");
        await T.WaitUntilAsync(() => client.State == ClientState.Connected && host.Players.Count == 1 && host.Players[0].IsConnected, "resumed");
        Assert.Equal(hello.PlayerId, host.Players[0].PlayerId);
        lock (resumedFlags) Assert.Equal(true, resumedFlags[^1]);
    }

    [Fact]
    public async Task Host_Marks_Player_Disconnected_But_Keeps_The_Seat()
    {
        var chaos = (ChaosTransport?)null;
        var rig = await Start(false, wrapClient: t => chaos = new ChaosTransport(t));
        await using var host = rig.Host;
        var opts = T.FastClient();
        opts.AutoReconnect = false;
        await using var client = rig.NewClient(opts);
        await client.JoinAsync(rig.Address, T.Hello());

        await chaos!.DropAllConnectionsAsync();
        await T.WaitUntilAsync(() => host.Players.Count == 1 && !host.Players[0].IsConnected, "player marked disconnected");
        Assert.Equal(PlayerLinkState.Disconnected, host.Players[0].State);
        Assert.Equal(ClientState.Disconnected, client.State);
    }

    [Fact]
    public async Task Known_Player_Can_Resume_Even_When_Session_Is_Closed_Or_Full()
    {
        var chaos = (ChaosTransport?)null;
        var rig = await Start(false, maxPlayers: 1, wrapClient: t => chaos = new ChaosTransport(t));
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        await client.JoinAsync(rig.Address, T.Hello());

        host.SetOpen(false);
        await chaos!.DropAllConnectionsAsync();
        await T.WaitUntilAsync(() => client.State == ClientState.Connected && host.Players[0].IsConnected, "resume despite closed+full");
    }

    [Fact]
    public async Task Host_Stop_Ends_The_Session_And_Client_Stays_Closed()
    {
        // Crash = the link is cut with no goodbye (a stopped host would say session.closing instead).
        var net = T.Net.Loopback();
        ChaosTransport? chaos = null;
        var host = new SessionHost(T.FastHost());
        await host.StartAsync(net.Transport, net.ListenAddress);
        host.SetOpen(true);
        chaos = new ChaosTransport(net.Transport);
        await using var client = new SessionClient(chaos, T.FastClient());
        var hello = T.Hello();
        await client.JoinAsync(net.ListenAddress, hello);

        // Take the address away, then cut the link: the client must freeze in Reconnecting.
        await host.DisposeAsync();
        // StopAsync says goodbye, so the client ends as SessionClosed (it may pass through Reconnecting first);
        // the no-goodbye crash path is covered by the drop / retry tests.
        await T.WaitUntilAsync(() => client.State == ClientState.SessionClosed, "client to see the session close");
        await Task.Delay(300);
        Assert.Equal(ClientState.SessionClosed, client.State);
    }

    [Fact]
    public async Task Client_Keeps_Retrying_Until_The_Host_Returns()
    {
        var net = T.Net.Loopback();
        ChaosTransport? chaos = null;
        var listener = await net.Transport.ListenAsync(net.ListenAddress);
        chaos = new ChaosTransport(net.Transport);
        await using var client = new SessionClient(chaos, T.FastClient());

        // Fake host: accept once, then vanish without a goodbye.
        _ = Task.Run(async () =>
        {
            var conn = (await SecureHandshake.AcceptAsync((await listener.AcceptAsync())!, null, null)).Connection!;
            var hello = EnvelopeCodec.Decode((await conn.ReceiveAsync())!)!;
            await conn.SendAsync(EnvelopeCodec.Encode(EnvelopeCodec.Create(NetMessageTypes.Accept,
                new AcceptMessage { SessionName = "x", HeartbeatSeconds = 1 }, hello.Id)));
        });
        var hello = T.Hello();
        Assert.True((await client.JoinAsync(net.ListenAddress, hello)).Accepted);
        listener.Stop();
        await chaos.DropAllConnectionsAsync();
        await T.WaitUntilAsync(() => client.State == ClientState.Reconnecting, "frozen");

        await Task.Delay(500);   // many failed attempts, still trying
        Assert.Equal(ClientState.Reconnecting, client.State);

        // The real host comes back on the same address
        var host = new SessionHost(T.FastHost());
        await host.StartAsync(net.Transport, net.ListenAddress);
        host.SetOpen(false);   // closed to NEW players, but this player is known only if it joined this host...
        host.SetOpen(true);
        await T.WaitUntilAsync(() => client.State == ClientState.Connected, "client to rejoin the returned host");
        await host.DisposeAsync();
    }

    [Fact]
    public async Task Silent_Host_Is_Detected_By_The_Watchdog()
    {
        // The link stays "open" but nothing flows: a half-dead connection. The client must notice and start reconnecting.
        var net = T.Net.Loopback();
        var listener = await net.Transport.ListenAsync(net.ListenAddress);
        var options = T.FastClient();
        options.HostSilenceTimeout = TimeSpan.FromMilliseconds(500);
        await using var client = new SessionClient(net.Transport, options);

        // A fake host that accepts the hello, answers with Accept, then goes silent.
        var fake = Task.Run(async () =>
        {
            var conn = (await SecureHandshake.AcceptAsync((await listener.AcceptAsync())!, null, null)).Connection!;
            var hello = EnvelopeCodec.Decode((await conn.ReceiveAsync())!)!;
            var accept = EnvelopeCodec.Create(NetMessageTypes.Accept, new AcceptMessage { SessionName = "x", HeartbeatSeconds = 1 }, hello.Id);
            await conn.SendAsync(EnvelopeCodec.Encode(accept));
            while (await conn.ReceiveAsync() != null) { }
        });

        var join = await client.JoinAsync(net.ListenAddress, T.Hello());
        Assert.True(join.Accepted);
        await T.WaitUntilAsync(() => client.State == ClientState.Reconnecting, "watchdog to notice silence", 4000);
    }

    [Fact]
    public async Task Request_Round_Trip_Host_To_Client_And_Back()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        var hello = T.Hello();
        await client.JoinAsync(rig.Address, hello);

        // Host asks the player something; the client app answers through MessageReceived.
        // Client needs a way to reply: its reply path is SessionClient.ReplyAsync.
        client.MessageReceived += async env =>
        {
            if (env.Type == "q") await client.ReplyAsync(env, "a", new NoticeMessage { Message = "answer" });
        };
        var reply = await host.RequestAsync(hello.PlayerId, "q", new NoticeMessage(), TimeSpan.FromSeconds(2));
        Assert.Equal("answer", EnvelopeCodec.ReadPayload<NoticeMessage>(reply)!.Message);

        // And the other way
        host.MessageReceived += async (p, env) =>
        {
            if (env.Type == "q2") await host.ReplyAsync(p.PlayerId, env, "a2", new NoticeMessage { Message = "from host" });
        };
        var back = await client.RequestAsync("q2", new NoticeMessage(), TimeSpan.FromSeconds(2));
        Assert.Equal("from host", EnvelopeCodec.ReadPayload<NoticeMessage>(back)!.Message);
    }

    [Fact]
    public async Task Eight_Players_Join_And_Everyone_Sees_The_Full_Roster()
    {
        var rig = await Start(false, maxPlayers: 8);
        await using var host = rig.Host;
        var clients = new List<SessionClient>();
        try
        {
            for (int i = 0; i < 8; i++)
            {
                var c = rig.NewClient();
                clients.Add(c);
                Assert.True((await c.JoinAsync(rig.Address, T.Hello("P" + i, "C" + i))).Accepted);
            }

            await T.WaitUntilAsync(() => clients.All(c => c.Roster != null && c.Roster.Players.Count == 8), "all rosters to show 8");
            await host.BroadcastAsync("hello-all", new NoticeMessage { Message = "hi" });
        }
        finally
        {
            foreach (var c in clients) await c.DisposeAsync();
        }
    }

    [Fact]
    public async Task Tcp_Drop_And_Resume_Works_On_Real_Sockets()
    {
        var chaos = (ChaosTransport?)null;
        var rig = await Start(true, wrapClient: t => chaos = new ChaosTransport(t));
        await using var host = rig.Host;
        await using var client = rig.NewClient();
        await client.JoinAsync(rig.Address, T.Hello());

        await chaos!.DropAllConnectionsAsync();
        await T.WaitUntilAsync(() => client.State == ClientState.Connected && host.Players.Count == 1 && host.Players[0].IsConnected, "tcp resume", 8000);
    }

    [Fact]
    public async Task Duplicate_And_Lossy_Network_Still_Joins()
    {
        var rig = await Start(false);
        await using var host = rig.Host;
        var faults = new ChaosTransport(rig.ClientTransport, new ChaosOptions { DuplicateRate = 0.5, LatencyMs = 5 });
        var options = T.FastClient();
        options.ConnectionDecorator = faults.Wrap;
        await using var client = rig.NewClient(options);
        Assert.True((await client.JoinAsync(rig.Address, T.Hello())).Accepted);
        Assert.Equal(1, host.Players.Count);
    }
}
