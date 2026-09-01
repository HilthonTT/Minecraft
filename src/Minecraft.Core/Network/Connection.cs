using Minecraft.Core.IO;
using Minecraft.Core.Network.Packets;
using System.Net.Sockets;

namespace Minecraft.Core.Network;

public sealed class Connection
{
    private readonly PacketFactory _packetFactory = new();

    public required TcpClient Client { get; init; }

    public required NetworkStream NetStream { get; init; }

    public required BinaryReader Reader { get; init; }

    public required BufferedDataStream Writer { get; init; }

    public void Close()
    {
        NetStream.Close();
        Client.Close();
    }

    public bool WritePacket(Packet packet)
    {
        packet.WriteToStream(Writer);
        return Writer.Flush();
    }

    public Packet ReadPacket(Session.Session session)
    {
        return _packetFactory.ReadPacket(session);
    }
}
