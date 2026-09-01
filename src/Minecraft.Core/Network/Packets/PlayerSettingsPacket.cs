using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerSettingsPacket : Packet
{
    public int ViewDistance { get; private set; }

    public PlayerSettingsPacket(int viewDistance) : base(PacketType.PlayerSettings)
    {
        ViewDistance = viewDistance;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerSettingsPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(ViewDistance);
    }
}
