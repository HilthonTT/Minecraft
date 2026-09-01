using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerAttackEntityPacket : Packet
{
    public int EntityID { get; private set; }

    public PlayerAttackEntityPacket(int entityId) : base(PacketType.PlayerAttackEntity)
    {
        EntityID = entityId;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerAttackEntityPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
    }
}
