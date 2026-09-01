using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerDropItemPacket : Packet
{
    public ushort ItemId { get; private set; }
    public int Count { get; private set; }

    public int Damage { get; private set; }

    public PlayerDropItemPacket(ushort itemId, int count, int damage) : base(PacketType.PlayerDropItem)
    {
        ItemId = itemId;
        Count = count;
        Damage = damage;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerDropItemPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteInt32(Count);
        bufferedStream.WriteInt32(Damage);
    }
}
