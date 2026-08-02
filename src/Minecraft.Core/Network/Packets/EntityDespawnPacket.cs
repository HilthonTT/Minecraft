using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Tells a client to forget an entity, either because it no longer exists or because it has moved out of
/// range. The client cannot tell the two apart, and has no reason to.
/// </summary>
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
