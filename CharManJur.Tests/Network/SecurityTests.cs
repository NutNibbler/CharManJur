using System.Text;
using CharManJur.Models;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class SecurityTests
{
    // Sits between the two ends of a link: sees (and can change) every frame, like someone on the Wi-Fi.
    private sealed class Spy : IConnection
    {
        private readonly IConnection _inner;
        public List<byte[]> Sent { get; } = new();
        public Func<byte[], byte[]>? Tamper { get; set; }

        public Spy(IConnection inner) => _inner = inner;
        public string RemoteDescription => _inner.RemoteDescription;
        public bool IsOpen => _inner.IsOpen;

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
        {
            var copy = frame.ToArray();
            Sent.Add(copy);
            return _inner.SendAsync(Tamper != null ? Tamper(copy) : copy, ct);
        }

        public Task<byte[]?> ReceiveAsync(CancellationToken ct = default) => _inner.ReceiveAsync(ct);
        public Task CloseAsync() => _inner.CloseAsync();
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }

    private sealed class Pair
    {
        public IConnection Client = null!;
        public IConnection Host = null!;
    }

    private static async Task<Pair> RawPair(Func<IConnection, IConnection>? wrapClient = null)
    {
        var net = T.Net.Loopback();
        var listener = await net.Transport.ListenAsync(net.ListenAddress);
        var accept = listener.AcceptAsync();
        var client = await net.Transport.ConnectAsync(net.ListenAddress);
        var host = (await accept)!;
        return new Pair { Client = wrapClient != null ? wrapClient(client) : client, Host = host };
    }

    private static async Task<(HandshakeResult Client, HandshakeResult Host)> Handshake(
        Pair pair, string? clientPassword, SessionCredentials? credentials, AuthLimiter? limiter = null,
        HandshakeOptions? clientOptions = null, HandshakeOptions? hostOptions = null)
    {
        var host = SecureHandshake.AcceptAsync(pair.Host, credentials, limiter, hostOptions);
        var client = SecureHandshake.ConnectAsync(pair.Client, clientPassword, clientOptions);
        return (await client, await host);
    }

    private static SessionCredentials Creds(string password) => SessionCredentials.FromPassword(password, iterations: SessionCredentials.MinIterations);

    // ------------------------------------------------------------ handshake

    [Fact]
    public async Task No_Password_Still_Encrypts_And_Carries_Data()
    {
        Spy? spy = null;
        var pair = await RawPair(c => spy = new Spy(c));
        var (client, host) = await Handshake(pair, null, null);
        Assert.True(client.Succeeded);
        Assert.True(host.Succeeded);

        await client.Connection!.SendAsync(Encoding.UTF8.GetBytes("secret hello"));
        var got = await host.Connection!.ReceiveAsync();
        Assert.Equal("secret hello", Encoding.UTF8.GetString(got!));

        await host.Connection!.SendAsync(Encoding.UTF8.GetBytes("and back"));
        Assert.Equal("and back", Encoding.UTF8.GetString((await client.Connection!.ReceiveAsync())!));

        // What went over the wire is not readable
        Assert.False(spy!.Sent.Any(f => Encoding.UTF8.GetString(f).Contains("secret hello")));
    }

    [Fact]
    public async Task Right_Password_Succeeds()
    {
        var pair = await RawPair();
        var (client, host) = await Handshake(pair, "dragons", Creds("dragons"));
        Assert.True(client.Succeeded);
        Assert.True(host.Succeeded);

        await client.Connection!.SendAsync(new byte[] { 1, 2, 3 });
        Assert.Equal(3, (await host.Connection!.ReceiveAsync())!.Length);
    }

    [Fact]
    public async Task Wrong_Password_Is_Refused_On_Both_Sides()
    {
        var pair = await RawPair();
        var (client, host) = await Handshake(pair, "wrong", Creds("dragons"));
        Assert.False(client.Succeeded);
        Assert.False(host.Succeeded);
        Assert.Equal(HandshakeFailure.BadPassword, client.Failure);
        Assert.Equal(HandshakeFailure.BadPassword, host.Failure);
    }

    [Fact]
    public async Task Missing_Password_Is_Reported()
    {
        var pair = await RawPair();
        var (client, _) = await Handshake(pair, null, Creds("dragons"));
        Assert.Equal(HandshakeFailure.PasswordRequired, client.Failure);
    }

    [Fact]
    public async Task Password_Is_Never_Sent()
    {
        Spy? spy = null;
        var pair = await RawPair(c => spy = new Spy(c));
        await Handshake(pair, "dragons-and-more", Creds("dragons-and-more"));
        Assert.False(spy!.Sent.Any(f => Encoding.UTF8.GetString(f).Contains("dragons-and-more")));
    }

    [Fact]
    public async Task Repeated_Wrong_Passwords_Lock_The_Address_Out()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new AuthLimiter(3, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), () => now);
        var creds = Creds("dragons");

        for (int i = 0; i < 3; i++)
        {
            var pair = await RawPair();
            var (_, host) = await Handshake(pair, "nope", creds, limiter);
            Assert.Equal(HandshakeFailure.BadPassword, host.Failure);
        }

        // Even the RIGHT password is now refused, without checking it
        var locked = await RawPair();
        var (client, lockedHost) = await Handshake(locked, "dragons", creds, limiter);
        Assert.Equal(HandshakeFailure.RateLimited, lockedHost.Failure);
        Assert.Equal(HandshakeFailure.RateLimited, client.Failure);

        // After the lockout it works again
        now = now.AddSeconds(61);
        var later = await RawPair();
        var (okClient, okHost) = await Handshake(later, "dragons", creds, limiter);
        Assert.True(okClient.Succeeded);
        Assert.True(okHost.Succeeded);
    }

    [Fact]
    public async Task Limiter_Success_Clears_The_Count()
    {
        var limiter = new AuthLimiter(3);
        var creds = Creds("dragons");

        for (int round = 0; round < 3; round++)
        {
            var bad = await RawPair();
            await Handshake(bad, "nope", creds, limiter);
            var good = await RawPair();
            var (c, _) = await Handshake(good, "dragons", creds, limiter);
            Assert.True(c.Succeeded);   // never reaches 3 wrong in a row
        }
    }

    [Fact]
    public async Task Tampered_Frame_Closes_The_Link()
    {
        Spy? spy = null;
        var pair = await RawPair(c => spy = new Spy(c));
        var (client, host) = await Handshake(pair, null, null);
        Assert.True(client.Succeeded);

        // The spy was only watching during the handshake; now it flips one bit of the next frame
        spy!.Tamper = f => { var copy = (byte[])f.Clone(); copy[copy.Length / 2] ^= 0x01; return copy; };
        await client.Connection!.SendAsync(Encoding.UTF8.GetBytes("attack at dawn"));

        Assert.Null(await host.Connection!.ReceiveAsync());
        Assert.False(host.Connection.IsOpen);
    }

    [Fact]
    public async Task Replayed_Frame_Is_Rejected()
    {
        Spy? spy = null;
        var pair = await RawPair(c => spy = new Spy(c));
        var (client, host) = await Handshake(pair, null, null);

        await client.Connection!.SendAsync(Encoding.UTF8.GetBytes("pay 100"));
        Assert.Equal("pay 100", Encoding.UTF8.GetString((await host.Connection!.ReceiveAsync())!));

        // The attacker sends the same captured frame again
        var captured = spy!.Sent.Last();
        await pair.Client.SendAsync(captured);
        Assert.Null(await host.Connection.ReceiveAsync());
    }

    [Fact]
    public async Task Garbage_Hello_Is_Refused_And_Does_Not_Crash_The_Host()
    {
        foreach (var junk in new[] { new byte[] { 1, 2, 3 }, new byte[0], Encoding.ASCII.GetBytes("CMJ1"), Encoding.ASCII.GetBytes("CMJ1\xFF\xFF\xFF\xFF"), new byte[200] })
        {
            var pair = await RawPair();
            var host = SecureHandshake.AcceptAsync(pair.Host, null, null);
            await pair.Client.SendAsync(junk);
            var result = await host;
            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public async Task Silent_Peer_Times_Out()
    {
        var pair = await RawPair();
        var result = await SecureHandshake.AcceptAsync(pair.Host, null, null, new HandshakeOptions { Timeout = TimeSpan.FromMilliseconds(150) });
        Assert.False(result.Succeeded);
    }

    // ------------------------------------------------------------ ciphers

    [Fact]
    public async Task ChaCha_Fallback_Works_When_Only_One_Side_Offers_It_First()
    {
        if (!Aead.IsSupported(CipherKind.ChaCha20Poly1305)) return;   // this machine (Windows 10) has no ChaCha: nothing to test
        var pair = await RawPair();
        var chaosOnly = new HandshakeOptions { Ciphers = new[] { CipherKind.ChaCha20Poly1305 } };
        var (client, host) = await Handshake(pair, "x", Creds("x"), clientOptions: chaosOnly);
        Assert.True(client.Succeeded);
        Assert.True(host.Succeeded);

        await client.Connection!.SendAsync(new byte[] { 9 });
        Assert.Equal(1, (await host.Connection!.ReceiveAsync())!.Length);
    }

    [Fact]
    public async Task No_Cipher_In_Common_Is_Incompatible()
    {
        if (!Aead.IsSupported(CipherKind.AesGcm) || !Aead.IsSupported(CipherKind.ChaCha20Poly1305)) return;   // cannot be tested on this machine

        var pair = await RawPair();
        var (client, host) = await Handshake(pair, null, null,
            clientOptions: new HandshakeOptions { Ciphers = new[] { CipherKind.AesGcm } },
            hostOptions: new HandshakeOptions { Ciphers = new[] { CipherKind.ChaCha20Poly1305 } });
        Assert.Equal(HandshakeFailure.Incompatible, client.Failure);
        Assert.Equal(HandshakeFailure.Incompatible, host.Failure);
    }

    [Fact]
    public void Aead_Round_Trip_And_Tamper_Detection()
    {
        foreach (var kind in new[] { CipherKind.AesGcm, CipherKind.ChaCha20Poly1305 })
        {
            if (!Aead.IsSupported(kind)) continue;
            var key = new byte[32];
            new Random(7).NextBytes(key);
            using var a = Aead.Create(kind, key);
            using var b = Aead.Create(kind, key);

            var sealedBytes = a.Seal(5, Encoding.UTF8.GetBytes("hello"));
            Assert.Equal("hello", Encoding.UTF8.GetString(b.Open(5, sealedBytes)!));
            Assert.Null(b.Open(6, sealedBytes));   // wrong counter

            sealedBytes[0] ^= 1;
            Assert.Null(b.Open(5, sealedBytes));   // tampered
            Assert.Null(b.Open(5, new byte[3]));   // too short
        }
    }

    // ------------------------------------------------------------ key schedule / credentials / limiter

    [Fact]
    public void KeySchedule_Directions_Differ_And_Depend_On_Every_Input()
    {
        var secret = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var pw = Enumerable.Range(100, 32).Select(i => (byte)i).ToArray();
        var cn = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var hn = Enumerable.Range(50, 32).Select(i => (byte)i).ToArray();

        var (c2h, h2c) = KeySchedule.Derive(secret, pw, cn, hn);
        Assert.Equal(32, c2h.Length);
        Assert.False(c2h.SequenceEqual(h2c));

        // Same inputs, same keys (both ends must agree)
        var again = KeySchedule.Derive(secret, pw, cn, hn);
        Assert.True(c2h.SequenceEqual(again.ClientToHost));

        // Change any one input and the keys change
        Assert.False(c2h.SequenceEqual(KeySchedule.Derive(secret, null, cn, hn).ClientToHost));
        Assert.False(c2h.SequenceEqual(KeySchedule.Derive(secret, pw, hn, cn).ClientToHost));
        var other = (byte[])secret.Clone(); other[0] ^= 1;
        Assert.False(c2h.SequenceEqual(KeySchedule.Derive(other, pw, cn, hn).ClientToHost));
    }

    [Fact]
    public void Credentials_Are_Deterministic_And_Validated()
    {
        var salt = new byte[SessionCredentials.SaltBytes];
        var a = SessionCredentials.FromPassword("pw", salt, SessionCredentials.MinIterations);
        var b = SessionCredentials.FromPassword("pw", salt, SessionCredentials.MinIterations);
        var c = SessionCredentials.FromPassword("pw2", salt, SessionCredentials.MinIterations);
        Assert.True(a.Key.SequenceEqual(b.Key));
        Assert.False(a.Key.SequenceEqual(c.Key));
        Assert.Equal(SessionCredentials.KeyBytes, a.Key.Length);

        var random1 = SessionCredentials.FromPassword("pw", iterations: SessionCredentials.MinIterations);
        var random2 = SessionCredentials.FromPassword("pw", iterations: SessionCredentials.MinIterations);
        Assert.False(random1.Salt.SequenceEqual(random2.Salt));
    }

    [Fact]
    public void Credentials_Match_The_Campaign_Password_Format()
    {
        // Campaign stores PBKDF2-SHA256(password, salt, 100 000 rounds, 32 bytes). Session credentials must equal that,
        // so the saved campaign password can be used for the session without storing anything new.
        var salt = new byte[16];
        new Random(5).NextBytes(salt);
        var campaignStyle = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "dragons ünïcode", salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);

        Assert.True(campaignStyle.SequenceEqual(SessionCredentials.Derive("dragons ünïcode", salt, 100_000)));
    }

    [Fact]
    public void Limiter_Key_Strips_The_Port()
    {
        Assert.Equal("192.168.1.5", AuthLimiter.KeyFor("192.168.1.5:50321"));
        Assert.Equal(AuthLimiter.KeyFor("10.0.0.2:1"), AuthLimiter.KeyFor("10.0.0.2:2"));
        Assert.NotEqual(AuthLimiter.KeyFor("10.0.0.2:1"), AuthLimiter.KeyFor("10.0.0.3:1"));
    }

    // ------------------------------------------------------------ whole session

    private static async Task<(SessionHost Host, T.Net Net)> StartHost(SessionCredentials? credentials, AuthLimiter? limiter = null)
    {
        var net = T.Net.Loopback();
        var options = T.FastHost();
        options.Credentials = credentials;
        options.AuthLimiter = limiter;
        var host = new SessionHost(options);
        await host.StartAsync(net.Transport, net.ListenAddress);
        host.SetOpen(true);
        return (host, net);
    }

    [Fact]
    public async Task Session_With_Password_Joins_And_Wrong_Password_Is_Rejected()
    {
        var (host, net) = await StartHost(Creds("dragons"));
        await using var _ = host;

        await using var good = new SessionClient(net.Transport, T.FastClient());
        var ok = await good.JoinAsync(net.ListenAddress, T.Hello("Ann"), "dragons");
        Assert.True(ok.Accepted);

        await using var bad = new SessionClient(net.Transport, T.FastClient());
        var refused = await bad.JoinAsync(net.ListenAddress, T.Hello("Bob"), "wrong");
        Assert.False(refused.Accepted);
        Assert.Equal(JoinRejectReason.WrongPassword, refused.Reason);

        await using var none = new SessionClient(net.Transport, T.FastClient());
        var missing = await none.JoinAsync(net.ListenAddress, T.Hello("Cy"));
        Assert.Equal(JoinRejectReason.WrongPassword, missing.Reason);

        Assert.Equal(1, host.Players.Count);
    }

    [Fact]
    public async Task Session_Lockout_Reports_Too_Many_Attempts()
    {
        var (host, net) = await StartHost(Creds("dragons"), new AuthLimiter(2));
        await using var _ = host;

        for (int i = 0; i < 2; i++)
        {
            await using var c = new SessionClient(net.Transport, T.FastClient());
            await c.JoinAsync(net.ListenAddress, T.Hello(), "wrong");
        }

        await using var locked = new SessionClient(net.Transport, T.FastClient());
        var result = await locked.JoinAsync(net.ListenAddress, T.Hello(), "dragons");
        Assert.Equal(JoinRejectReason.TooManyAttempts, result.Reason);
    }

    [Fact]
    public async Task Dropped_Link_Resumes_With_The_Remembered_Password()
    {
        var (host, net) = await StartHost(Creds("dragons"));
        await using var _ = host;

        var chaos = new ChaosTransport(net.Transport);
        await using var client = new SessionClient(chaos, T.FastClient());
        Assert.True((await client.JoinAsync(net.ListenAddress, T.Hello(), "dragons")).Accepted);

        await chaos.DropAllConnectionsAsync();
        await T.WaitUntilAsync(() => client.State == ClientState.Connected && host.Players.Count == 1 && host.Players[0].IsConnected, "resume with password");
    }

    [Fact]
    public async Task Session_Traffic_Is_Not_Readable_On_The_Wire()
    {
        var net = T.Net.Loopback();
        var host = new SessionHost(T.FastHost());
        await using var _ = host;
        await host.StartAsync(net.Transport, net.ListenAddress);
        host.SetOpen(true);

        var spies = new List<Spy>();
        var options = T.FastClient();
        options.ConnectionDecorator = c => c;   // decrypted link is untouched
        var spyTransport = new SpyTransport(net.Transport, spies);
        await using var client = new SessionClient(spyTransport, options);
        Assert.True((await client.JoinAsync(net.ListenAddress, T.Hello("Zebulon", "Quillfeather"))).Accepted);

        var wire = string.Concat(spies.SelectMany(s => s.Sent).Select(f => Encoding.UTF8.GetString(f)));
        Assert.False(wire.Contains("Zebulon"));
        Assert.False(wire.Contains("Quillfeather"));
    }

    private sealed class SpyTransport : ITransport
    {
        private readonly ITransport _inner;
        private readonly List<Spy> _spies;
        public SpyTransport(ITransport inner, List<Spy> spies) { _inner = inner; _spies = spies; }
        public Task<IListener> ListenAsync(string address, CancellationToken ct = default) => _inner.ListenAsync(address, ct);

        public async Task<IConnection> ConnectAsync(string address, CancellationToken ct = default)
        {
            var spy = new Spy(await _inner.ConnectAsync(address, ct));
            lock (_spies) _spies.Add(spy);
            return spy;
        }
    }
}