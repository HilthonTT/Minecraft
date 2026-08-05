using Minecraft.Core.Logging;
using System.Net;
using System.Net.Sockets;

namespace Minecraft.Core.Network;

/// <summary>Works out how somebody else on the same network would reach a world hosted here.</summary>
public static class NetworkAddresses
{
    private static string? _localAddress;

    /// <summary>
    /// This machine's address on the local network, or the loopback address when it has none. Looked up
    /// once, since it means asking the operating system and it does not change while the game runs.
    /// </summary>
    public static string LocalAddress => _localAddress ??= FindLocalAddress();

    private static string FindLocalAddress()
    {
        try
        {
            // Opening a socket towards an outside address picks the interface the machine would actually
            // route through, which is the one a friend on the same network can reach. Nothing is sent: a
            // datagram socket only records where it would go.
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch (SocketException e)
        {
            // A machine with no network at all can still host for players on the same computer.
            Logger.Info("Could not work out the local network address -> " + e.Message);
        }

        return IPAddress.Loopback.ToString();
    }
}
