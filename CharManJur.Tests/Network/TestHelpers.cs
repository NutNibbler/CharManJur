using System.Net;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Tests.Network;

internal static class T
{
    // Polls until the condition holds (or fails the test after the timeout).
    public static async Task WaitUntilAsync(Func<bool> condition, string what, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new Xunit.Sdk.XunitException("Timed out waiting for: " + what);
    }

    public static async Task<byte[]?> ReceiveWithin(IConnection connection, int ms)
    {
        var receive = connection.ReceiveAsync();
        var done = await Task.WhenAny(receive, Task.Delay(ms));
        return done == receive ? await receive : new byte[] { 0xFF, 0xFF, 0xFF };   // 0xFFFFFF = "nothing arrived"
    }

    public static bool NothingArrived(byte[]? frame) => frame != null && frame.Length == 3 && frame[0] == 0xFF && frame[1] == 0xFF;

    public static HelloMessage Hello(string player = "Ann", string character = "Aria", Guid? playerId = null, Guid? characterGuid = null)
        => new()
        {
            AppVersion = "test",
            PlayerId = playerId ?? Guid.NewGuid(),
            PlayerName = player,
            CharacterGuid = characterGuid ?? Guid.NewGuid(),
            CharacterName = character
        };

    // A transport pair for one test: either in-memory or real TCP on localhost.
    public sealed class Net
    {
        public ITransport Transport { get; init; } = null!;
        public string ListenAddress { get; init; } = string.Empty;
        public bool IsTcp { get; init; }

        public static Net Loopback() => new()
        {
            Transport = new LoopbackTransport(),
            ListenAddress = "test-" + Guid.NewGuid().ToString("N")
        };

        public static Net Tcp() => new() { Transport = new TcpTransport(), ListenAddress = "127.0.0.1:0", IsTcp = true };

        // Address a client uses to reach a listener started on ListenAddress
        public string ClientAddress(IListener listener)
        {
            if (!IsTcp) return ListenAddress;
            var local = listener.LocalDescription;
            return "127.0.0.1:" + local[(local.LastIndexOf(':') + 1)..];
        }

        public string ClientAddress(SessionHost host)
        {
            if (!IsTcp) return ListenAddress;
            var local = host.LocalDescription!;
            return "127.0.0.1:" + local[(local.LastIndexOf(':') + 1)..];
        }
    }

    public static SessionHostOptions FastHost(int maxPlayers = 8) => new()
    {
        SessionName = "Test Campaign",
        MaxPlayers = maxPlayers,
        HeartbeatInterval = TimeSpan.FromMilliseconds(50),
        HeartbeatTimeout = TimeSpan.FromMilliseconds(600),
        HandshakeTimeout = TimeSpan.FromSeconds(2)
    };

    public static SessionClientOptions FastClient() => new()
    {
        ConnectTimeout = TimeSpan.FromSeconds(2),
        HandshakeTimeout = TimeSpan.FromSeconds(2),
        HostSilenceTimeout = TimeSpan.FromSeconds(3),
        ReconnectDelays = new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100) }
    };
}
