using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerKeepAlivePacket : Packet
{
    public PlayerKeepAlivePacket() : base(PacketType.PlayerKeepAlive) { }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerKeepAlivePacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream) { }
}
