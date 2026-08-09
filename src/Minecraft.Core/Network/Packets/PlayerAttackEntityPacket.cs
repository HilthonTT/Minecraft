using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// A player swinging at an entity. Carries nothing but who was hit: what a blow is worth, whether the target
/// was close enough to reach and whether it survived are all the server's to decide.
/// </summary>
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
