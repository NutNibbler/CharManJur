using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class ChaosAndChannelTests
{
    private static async Task<(IConnection Client, IConnection Server)> Pair(ITransport transport, string address = "pair")
    {
        var listener = await transport.ListenAsync(address);
        var client = await transport.ConnectAsync(address);
        var server = (await listener.AcceptAsync())!;
        return (client, server);
    }

    [Fact]
    public async Task Chaos_Forced_Disconnect_After_N_Frames()
    {
        var chaos = new ChaosTransport(new LoopbackTransport(), new ChaosOptions { DisconnectAfterFrames = 2 });
        var (client, server) = await Pair(chaos);

        await client.SendAsync(new byte[] { 1 });
        await client.SendAsync(new byte[] { 2 });
        await Assert.ThrowsAnyAsync<IOException>(() => client.SendAsync(new byte[] { 3 }));

        Assert.NotNull(await server.ReceiveAsync());
        Assert.NotNull(await server.ReceiveAsync());
        Assert.Null(await server.ReceiveAsync());   // the link ended
    }

    [Fact]
    public async Task Chaos_Duplicates_Frames()
    {
        var chaos = new ChaosTransport(new LoopbackTransport(), new ChaosOptions { DuplicateRate = 1.0 });
        var (client, server) = await Pair(chaos);

        await client.SendAsync(new byte[] { 5 });
        Assert.NotNull(await T.ReceiveWithin(server, 500));
        var second = await T.ReceiveWithin(server, 500);
        Assert.False(T.NothingArrived(second));
        Assert.True(T.NothingArrived(await T.ReceiveWithin(server, 150)));
    }

    [Fact]
    public async Task Chaos_Drops_Frames()
    {
        var chaos = new ChaosTransport(new LoopbackTransport(), new ChaosOptions { DropRate = 1.0 });
        var (client, server) = await Pair(chaos);

        await client.SendAsync(new byte[] { 5 });
        Assert.True(T.NothingArrived(await T.ReceiveWithin(server, 200)));
    }

    [Fact]
    public async Task Chaos_Adds_Latency()
    {
        var chaos = new ChaosTransport(new LoopbackTransport(), new ChaosOptions { LatencyMs = 150 });
        var (client, server) = await Pair(chaos);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await client.SendAsync(new byte[] { 1 });
        Assert.True(sw.ElapsedMilliseconds >= 120, "latency not applied");
        Assert.NotNull(await server.ReceiveAsync());
    }

    [Fact]
    public async Task Chaos_DropAllConnections_Kills_The_Link()
    {
        var chaos = new ChaosTransport(new LoopbackTransport());
        var (client, server) = await Pair(chaos);

        await chaos.DropAllConnectionsAsync();
        Assert.Null(await server.ReceiveAsync());
        Assert.False(client.IsOpen);
    }

    private static async Task<(NetChannel A, NetChannel B)> Channels()
    {
        var (c, s) = await Pair(new LoopbackTransport(), "chan-" + Guid.NewGuid().ToString("N"));
        var a = new NetChannel(c, Guid.NewGuid());
        var b = new NetChannel(s, Guid.NewGuid());
        return (a, b);
    }

    [Fact]
    public async Task Channel_Request_Gets_Its_Reply()
    {
        var (a, b) = await Channels();
        b.Handler = async env => await b.ReplyAsync(env, "pong-test", new NoticeMessage { Message = "hi " + EnvelopeCodec.ReadPayload<NoticeMessage>(env)!.Message });
        a.Start(); b.Start();

        var reply = await a.RequestAsync("ask", new NoticeMessage { Message = "Ann" }, TimeSpan.FromSeconds(2));
        Assert.Equal("hi Ann", EnvelopeCodec.ReadPayload<NoticeMessage>(reply)!.Message);
        Assert.Equal(reply.Corr, reply.Corr);
    }

    [Fact]
    public async Task Channel_Handler_Can_Issue_Its_Own_Request_Without_Deadlock()
    {
        var (a, b) = await Channels();
        // A asks B something; while answering, B asks A something and waits for it.
        a.Handler = async env => await a.ReplyAsync(env, "inner-reply", new NoticeMessage { Message = "from A" });
        b.Handler = async env =>
        {
            var inner = await b.RequestAsync("inner", new NoticeMessage(), TimeSpan.FromSeconds(2));
            await b.ReplyAsync(env, "outer-reply", new NoticeMessage { Message = "B got: " + EnvelopeCodec.ReadPayload<NoticeMessage>(inner)!.Message });
        };
        a.Start(); b.Start();

        var reply = await a.RequestAsync("outer", new NoticeMessage(), TimeSpan.FromSeconds(3));
        Assert.Equal("B got: from A", EnvelopeCodec.ReadPayload<NoticeMessage>(reply)!.Message);
    }

    [Fact]
    public async Task Channel_Request_Times_Out()
    {
        var (a, b) = await Channels();
        b.Handler = _ => Task.CompletedTask;   // never answers
        a.Start(); b.Start();
        await Assert.ThrowsAnyAsync<TimeoutException>(() => a.RequestAsync("ask", new NoticeMessage(), TimeSpan.FromMilliseconds(150)));
    }

    [Fact]
    public async Task Channel_Close_Fails_Pending_Requests()
    {
        var (a, b) = await Channels();
        b.Handler = _ => Task.CompletedTask;
        a.Start(); b.Start();

        var pending = a.RequestAsync("ask", new NoticeMessage(), TimeSpan.FromSeconds(5));
        await b.CloseAsync();
        await Assert.ThrowsAnyAsync<IOException>(() => pending);
        await T.WaitUntilAsync(() => !a.IsOpen, "the other channel to notice");
    }

    [Fact]
    public async Task Channel_Stamps_Sender_And_Increasing_Sequence()
    {
        var (a, b) = await Channels();
        var seen = new List<Envelope>();
        b.Handler = env => { lock (seen) seen.Add(env); return Task.CompletedTask; };
        a.Start(); b.Start();

        for (int i = 0; i < 3; i++) await a.SendAsync("n", new NoticeMessage { Message = i.ToString() });
        await T.WaitUntilAsync(() => { lock (seen) return seen.Count == 3; }, "three messages");

        Assert.True(seen[0].Seq < seen[1].Seq && seen[1].Seq < seen[2].Seq);
        Assert.NotEqual(Guid.Empty, seen[0].From);
        Assert.Equal("0", EnvelopeCodec.ReadPayload<NoticeMessage>(seen[0])!.Message);   // arrives in order
        Assert.Equal("2", EnvelopeCodec.ReadPayload<NoticeMessage>(seen[2])!.Message);
    }

    [Fact]
    public async Task Channel_Garbage_Frames_Are_Ignored()
    {
        var (c, s) = await Pair(new LoopbackTransport(), "garbage");
        var b = new NetChannel(s, Guid.NewGuid());
        var got = new List<string>();
        b.Handler = env => { lock (got) got.Add(env.Type); return Task.CompletedTask; };
        b.Start();

        await c.SendAsync(System.Text.Encoding.UTF8.GetBytes("###"));
        await c.SendAsync(EnvelopeCodec.Encode(EnvelopeCodec.Create("real", new NoticeMessage())));

        await T.WaitUntilAsync(() => { lock (got) return got.Count == 1; }, "the real message");
        Assert.Equal("real", got[0]);
    }
}
