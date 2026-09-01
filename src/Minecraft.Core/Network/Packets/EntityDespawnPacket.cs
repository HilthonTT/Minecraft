using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class EntityDespawnPacket : Packet
{
    public int EntityID { get; private set; }

    public EntityDespawnPacket(int entityId) : base(PacketType.EntityDespawn)
    {
        EntityID = entityId;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntityDespawnPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
    }
}
