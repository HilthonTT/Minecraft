using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class EntityHurtPacket : Packet
{
    public int EntityID { get; private set; }

    public bool Died { get; private set; }

    public EntityHurtPacket(int entityId, bool died) : base(PacketType.EntityHurt)
    {
        EntityID = entityId;
        Died = died;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntityHurtPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteBool(Died);
    }
}
