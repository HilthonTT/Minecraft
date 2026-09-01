using Minecraft.Core.Logging;
using System.Net;
using System.Net.Sockets;

namespace Minecraft.Core.Network;

public static class NetworkAddresses
{
    private static string? _localAddress;

    public static string LocalAddress => _localAddress ??= FindLocalAddress();

    private static string FindLocalAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch (SocketException e)
        {
            Logger.Info("Could not work out the local network address -> " + e.Message);
        }

        return IPAddress.Loopback.ToString();
    }
}
