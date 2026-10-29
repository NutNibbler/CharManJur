using System.Net.Sockets;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class TransportTests
{
    [Fact] public Task Loopback_RoundTrip() => RoundTrip(T.Net.Loopback());
    [Fact] public Task Tcp_RoundTrip() => RoundTrip(T.Net.Tcp());

    [Fact] public Task Loopback_Close_Is_Detected() => CloseDetected(T.Net.Loopback());
    [Fact] public Task Tcp_Close_Is_Detected() => CloseDetected(T.Net.Tcp());

    [Fact] public Task Loopback_Listener_Stop_Ends_Accept() => ListenerStop(T.Net.Loopback());
    [Fact] public Task Tcp_Listener_Stop_Ends_Accept() => ListenerStop(T.Net.Tcp());

    [Fact]
    public async Task Loopback_Refuses_When_Nobody_Listens()
    {
        await Assert.ThrowsAnyAsync<IOException>(() => new LoopbackTransport().ConnectAsync("nobody"));
    }

    [Fact]
    public async Task Loopback_Address_Cannot_Be_Used_Twice()
    {
        var t = new LoopbackTransport();
        await t.ListenAsync("a");
        await Assert.ThrowsAnyAsync<IOException>(() => t.ListenAsync("a"));
    }

    [Fact]
    public async Task Tcp_Hostile_Length_Header_Closes_The_Link()
    {
        var net = T.Net.Tcp();
        await using var listener = await net.Transport.ListenAsync(net.ListenAddress);
        var port = int.Parse(listener.LocalDescription[(listener.LocalDescription.LastIndexOf(':') + 1)..]);

        using var raw = new TcpClient();
        await raw.ConnectAsync("127.0.0.1", port);
        var server = (await listener.AcceptAsync())!;

        await raw.GetStream().WriteAsync(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 1, 2, 3 });
        Assert.Null(await server.ReceiveAsync());
        Assert.False(server.IsOpen);
    }

    [Fact]
    public void Tcp_Address_Parsing()
    {
        Assert.Equal(("", 47200), TcpTransport.ParseAddress(""));
        Assert.Equal(("", 5000), TcpTransport.ParseAddress("5000"));
        Assert.Equal(("", 5000), TcpTransport.ParseAddress(":5000"));
        Assert.Equal(("192.168.1.5", 47200), TcpTransport.ParseAddress("192.168.1.5"));
        Assert.Equal(("192.168.1.5", 6000), TcpTransport.ParseAddress("192.168.1.5:6000"));
    }

    private static async Task RoundTrip(T.Net net)
    {
        await using var listener = await net.Transport.ListenAsync(net.ListenAddress);
        await using var client = await net.Transport.ConnectAsync(net.ClientAddress(listener));
        await using var server = (await listener.AcceptAsync())!;

        await client.SendAsync(new byte[] { 1, 2, 3 });
        await client.SendAsync(new byte[] { 4 });
        Assert.Equal(new byte[] { 1, 2, 3 }.Length, (await server.ReceiveAsync())!.Length);
        Assert.Equal((byte)4, (await server.ReceiveAsync())![0]);

        await server.SendAsync(new byte[] { 7, 7 });
        Assert.Equal(2, (await client.ReceiveAsync())!.Length);
    }

    private static async Task CloseDetected(T.Net net)
    {
        await using var listener = await net.Transport.ListenAsync(net.ListenAddress);
        await using var client = await net.Transport.ConnectAsync(net.ClientAddress(listener));
        await using var server = (await listener.AcceptAsync())!;

        await client.CloseAsync();
        Assert.Null(await server.ReceiveAsync());
        await T.WaitUntilAsync(() => !server.IsOpen, "server side to see the close");
    }

    private static async Task ListenerStop(T.Net net)
    {
        var listener = await net.Transport.ListenAsync(net.ListenAddress);
        var pending = listener.AcceptAsync();
        listener.Stop();
        var done = await Task.WhenAny(pending, Task.Delay(2000));
        Assert.True(done == pending, "Accept did not end after Stop");
        Assert.Null(await pending);
    }
}
