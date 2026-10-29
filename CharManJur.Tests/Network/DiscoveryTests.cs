using System.Net;
using System.Net.Sockets;
using CharManJur.Services;
using Xunit;

namespace CharManJur.Tests.Network;

public class DiscoveryTests
{
    private static readonly IPAddress[] LoopbackOnly = { IPAddress.Loopback };

    // A free UDP port for one test, so tests never collide with each other or with a running app
    private static int FreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

    private static DiscoveryOptions Options(int? port = null) => new()
    {
        Port = port ?? FreeUdpPort(),
        Interval = TimeSpan.FromMilliseconds(50),
        Expiry = TimeSpan.FromMilliseconds(400),
        Targets = LoopbackOnly
    };

    private static SessionAnnouncement Announce(string name = "Friday Night", int players = 2, bool password = true, bool open = true, Guid? id = null)
        => new(id ?? Guid.NewGuid(), name, 47200, password, open, players, 8);

    // ------------------------------------------------------------ invite codes

    [Fact]
    public void Invite_Code_Round_Trips()
    {
        var random = new Random(11);
        for (int i = 0; i < 500; i++)
        {
            var bytes = new byte[4];
            random.NextBytes(bytes);
            var ip = new IPAddress(bytes);
            int port = random.Next(1, 65536);
            bool password = random.Next(2) == 0;

            var code = InviteCode.Encode(ip, port, password);
            Assert.Equal(14, code.Length);   // 12 characters and 2 dashes

            var decoded = InviteCode.TryDecode(code);
            Assert.NotNull(decoded);
            Assert.Equal(ip.ToString(), decoded!.Address.ToString());
            Assert.Equal(port, decoded.Port);
            Assert.Equal(password, decoded.PasswordRequired);
        }
    }

    [Fact]
    public void Invite_Code_Gives_A_Connectable_Address()
    {
        var code = InviteCode.Encode(IPAddress.Parse("192.168.1.25"), 47200, false);
        Assert.Equal("192.168.1.25:47200", InviteCode.TryDecode(code)!.HostAddress);
    }

    [Fact]
    public void Invite_Code_Forgives_Case_Spaces_And_Lookalikes()
    {
        var code = InviteCode.Encode(IPAddress.Parse("10.0.0.7"), 5000, true);
        var sloppy = code.ToLowerInvariant().Replace("-", "  ").Replace('0', 'o').Replace('1', 'l');
        var decoded = InviteCode.TryDecode(sloppy);
        Assert.NotNull(decoded);
        Assert.Equal("10.0.0.7:5000", decoded!.HostAddress);
    }

    [Fact]
    public void Invite_Code_Catches_Every_Single_Typo()
    {
        foreach (var ip in new[] { "192.168.1.25", "10.20.30.40", "172.16.5.9" })
        {
            var code = InviteCode.Encode(IPAddress.Parse(ip), 47200, true).Replace("-", "");
            for (int position = 0; position < code.Length; position++)
            {
                foreach (var other in InviteCode.Alphabet)
                {
                    if (other == code[position]) continue;
                    var typo = code[..position] + other + code[(position + 1)..];
                    Assert.Null(InviteCode.TryDecode(typo));
                }
            }
        }
    }

    [Fact]
    public void Invite_Code_Rejects_Junk()
    {
        foreach (var junk in new[] { null, "", "   ", "ABCD", "ABCD-EFGH-JKMN-PQRS", "UUUU-UUUU-UUUU", "hello world!!!", "0000-0000-0000" })
            Assert.Null(InviteCode.TryDecode(junk));
    }

    [Fact]
    public void Invite_Code_Rejects_Bad_Input_To_Encode()
    {
        Assert.True(Throws(() => InviteCode.Encode(IPAddress.Parse("192.168.1.1"), 0, false)));
        Assert.True(Throws(() => InviteCode.Encode(IPAddress.IPv6Loopback, 80, false)));
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }

    // ------------------------------------------------------------ announcement format

    [Fact]
    public void Announcement_Round_Trips()
    {
        var original = Announce("Curse of the Crimson Throne", 3, true, false);
        var decoded = SessionAnnouncement.TryDecode(original.Encode());
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Announcement_Long_Names_Are_Clipped()
    {
        var decoded = SessionAnnouncement.TryDecode(Announce(new string('x', 500)).Encode());
        Assert.NotNull(decoded);
        Assert.Equal(SessionAnnouncement.MaxNameChars, decoded!.SessionName.Length);
    }

    [Fact]
    public void Announcement_Garbage_Never_Throws_And_Never_Decodes()
    {
        var random = new Random(3);
        for (int i = 0; i < 2000; i++)
        {
            var junk = new byte[random.Next(0, 200)];
            random.NextBytes(junk);
            Assert.Null(SessionAnnouncement.TryDecode(junk));
        }

        // Every truncation of a good announcement is refused, and so is any flipped magic byte
        var good = Announce().Encode();
        for (int length = 0; length < good.Length; length++)
            Assert.Null(SessionAnnouncement.TryDecode(good.AsSpan(0, length)));

        var bad = (byte[])good.Clone();
        bad[0] ^= 1;
        Assert.Null(SessionAnnouncement.TryDecode(bad));
    }

    [Fact]
    public void Announcement_Control_Characters_Are_Removed()
    {
        var decoded = SessionAnnouncement.TryDecode(Announce("Game\u0007\r\nNight").Encode());
        Assert.Equal("GameNight", decoded!.SessionName);
    }

    // ------------------------------------------------------------ announcer and finder over real UDP

    [Fact]
    public async Task Finder_Hears_An_Announcer()
    {
        var options = Options();
        var announcement = Announce("Friday Night", 2);
        await using var finder = new SessionFinder(options);
        finder.Start();
        await using var announcer = new SessionAnnouncer(() => announcement, options);
        announcer.Start();

        await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "the session to be found");
        var found = finder.Sessions[0];
        Assert.Equal("Friday Night", found.Announcement.SessionName);
        Assert.Equal("127.0.0.1:47200", found.Address);
        Assert.True(found.Announcement.PasswordRequired);
        Assert.Equal(announcement.SessionId, found.Announcement.SessionId);

        // The invite code shown next to the session decodes back to the same place
        Assert.Equal(found.Address, InviteCode.TryDecode(found.InviteCode)!.HostAddress);
    }

    [Fact]
    public async Task Finder_Sees_Updated_Details()
    {
        var options = Options();
        var id = Guid.NewGuid();
        var players = 1;
        await using var finder = new SessionFinder(options);
        finder.Start();
        await using var announcer = new SessionAnnouncer(() => Announce("Table", players, id: id), options);
        announcer.Start();

        await T.WaitUntilAsync(() => finder.Sessions.Count == 1 && finder.Sessions[0].Announcement.PlayerCount == 1, "first announcement");
        players = 4;
        await T.WaitUntilAsync(() => finder.Sessions.Count == 1 && finder.Sessions[0].Announcement.PlayerCount == 4, "player count update");
    }

    [Fact]
    public async Task Session_Disappears_After_The_Announcer_Stops()
    {
        var options = Options();
        await using var finder = new SessionFinder(options);
        finder.Start();
        var announcer = new SessionAnnouncer(() => Announce(), options);
        announcer.Start();

        await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "found");
        await announcer.StopAsync();
        await T.WaitUntilAsync(() => finder.Sessions.Count == 0, "the stale session to expire", 3000);
    }

    [Fact]
    public async Task Two_Sessions_Are_Listed_Separately_And_Changed_Fires()
    {
        var options = Options();
        int changes = 0;
        await using var finder = new SessionFinder(options);
        finder.Changed += () => Interlocked.Increment(ref changes);
        finder.Start();
        await using var a = new SessionAnnouncer(() => Announce("Alpha"), options);
        await using var b = new SessionAnnouncer(() => Announce("Bravo"), options);
        a.Start();
        b.Start();

        await T.WaitUntilAsync(() => finder.Sessions.Count == 2, "both sessions");
        Assert.Equal("Alpha", finder.Sessions[0].Announcement.SessionName);
        Assert.Equal("Bravo", finder.Sessions[1].Announcement.SessionName);
        Assert.True(Volatile.Read(ref changes) >= 2);
    }

    [Fact]
    public async Task Announcer_Stays_Quiet_When_There_Is_Nothing_To_Announce()
    {
        var options = Options();
        await using var finder = new SessionFinder(options);
        finder.Start();
        await using var announcer = new SessionAnnouncer(() => null, options);
        announcer.Start();

        await Task.Delay(300);
        Assert.Equal(0, finder.Sessions.Count);
    }

    [Fact]
    public async Task Junk_And_Floods_Are_Ignored()
    {
        var options = Options();
        options.MaxSessions = 5;
        await using var finder = new SessionFinder(options);
        finder.Start();

        using var attacker = new UdpClient();
        var target = new IPEndPoint(IPAddress.Loopback, options.Port);
        var junk = new byte[] { 1, 2, 3, 4, 5 };
        await attacker.SendAsync(junk, junk.Length, target);

        for (int i = 0; i < 40; i++)
        {
            var fake = Announce("Fake " + i).Encode();
            await attacker.SendAsync(fake, fake.Length, target);
        }

        await T.WaitUntilAsync(() => finder.Sessions.Count == 5, "the list to fill");
        await Task.Delay(200);
        Assert.Equal(5, finder.Sessions.Count);   // capped, no crash
    }

    [Fact]
    public async Task Finder_Can_Stop_And_Start_Again()
    {
        var options = Options();
        await using var announcer = new SessionAnnouncer(() => Announce("Again"), options);
        announcer.Start();

        var finder = new SessionFinder(options);
        finder.Start();
        await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "first run");
        await finder.StopAsync();
        Assert.Equal(0, finder.Sessions.Count);

        finder.Start();
        await T.WaitUntilAsync(() => finder.Sessions.Count == 1, "second run");
        await finder.DisposeAsync();
    }

    [Fact]
    public async Task Two_Finders_Can_Share_The_Port()
    {
        // Two copies of the app on one PC (the single-machine test setup) must both be able to listen
        var options = Options();
        await using var first = new SessionFinder(options);
        await using var second = new SessionFinder(options);
        first.Start();
        second.Start();
        Assert.True(first.IsRunning && second.IsRunning);
    }

    [Fact]
    public void Local_Addresses_Never_Throw()
    {
        var lan = LocalAddresses.LanIPv4();
        Assert.True(lan.All(a => a.AddressFamily == AddressFamily.InterNetwork));
        Assert.True(LocalAddresses.BroadcastTargets().Contains(IPAddress.Broadcast));
    }
}
