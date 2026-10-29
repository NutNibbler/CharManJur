using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CharManJur.Services;

// Which addresses this device can be reached on, for the invite code and the broadcast.
public static class LocalAddresses
{
    // Usable IPv4 addresses, best first: home/office ranges (192.168.x, 10.x, 172.16-31.x) before anything else.
    public static IReadOnlyList<IPAddress> LanIPv4()
    {
        var found = new List<(IPAddress Address, int Rank)>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                foreach (var info in nic.GetIPProperties().UnicastAddresses)
                {
                    var address = info.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IsLinkLocal(address)) continue;   // 169.254.x.x: no real network
                    found.Add((address, Rank(address)));
                }
            }
        }
        catch (Exception ex)
        {
            NetLog.Warn("could not list network adapters: " + ex.Message);
        }

        return found.OrderBy(f => f.Rank).Select(f => f.Address).Distinct().ToList();
    }

    // The broadcast address of each connected network (e.g. 192.168.1.255), plus the general one.
    public static IReadOnlyList<IPAddress> BroadcastTargets()
    {
        var targets = new List<IPAddress>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                foreach (var info in nic.GetIPProperties().UnicastAddresses)
                {
                    if (info.Address.AddressFamily != AddressFamily.InterNetwork || info.IPv4Mask == null) continue;
                    if (IsLinkLocal(info.Address)) continue;

                    var ip = info.Address.GetAddressBytes();
                    var mask = info.IPv4Mask.GetAddressBytes();
                    var broadcast = new byte[4];
                    for (int i = 0; i < 4; i++) broadcast[i] = (byte)(ip[i] | ~mask[i]);
                    targets.Add(new IPAddress(broadcast));
                }
            }
        }
        catch (Exception ex)
        {
            NetLog.Warn("could not work out broadcast addresses: " + ex.Message);
        }

        targets.Add(IPAddress.Broadcast);
        return targets.Distinct().ToList();
    }

    private static bool IsLinkLocal(IPAddress a)
    {
        var b = a.GetAddressBytes();
        return b[0] == 169 && b[1] == 254;
    }

    private static int Rank(IPAddress a)
    {
        var b = a.GetAddressBytes();
        if (b[0] == 192 && b[1] == 168) return 0;
        if (b[0] == 10) return 1;
        if (b[0] == 172 && b[1] is >= 16 and <= 31) return 2;
        return 3;
    }
}