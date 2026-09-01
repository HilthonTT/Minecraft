using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class ItemSpawnPacket : Packet
{
    public int EntityID { get; private set; }
    public Vector3 Position { get; private set; }
    public ushort ItemId { get; private set; }
    public int Count { get; private set; }
    public int Damage { get; private set; }

    public ItemSpawnPacket(int entityId, Vector3 position, ushort itemId, int count, int damage)
        : base(PacketType.ItemSpawn)
    {
        EntityID = entityId;
        Position = position;
        ItemId = itemId;
        Count = count;
        Damage = damage;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessItemSpawnPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteVector3(Position);
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteInt32(Count);
        bufferedStream.WriteInt32(Damage);
    }
}
