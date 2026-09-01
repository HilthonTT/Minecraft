using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class ItemPickupPacket : Packet
{
    public ushort ItemId { get; private set; }
    public int Count { get; private set; }

    public int Damage { get; private set; }

    public int EntityID { get; private set; }

    public ItemPickupPacket(int entityId, ushort itemId, int count, int damage) : base(PacketType.ItemPickup)
    {
        EntityID = entityId;
        ItemId = itemId;
        Count = count;
        Damage = damage;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessItemPickupPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteInt32(Count);
        bufferedStream.WriteInt32(Damage);
    }
}
