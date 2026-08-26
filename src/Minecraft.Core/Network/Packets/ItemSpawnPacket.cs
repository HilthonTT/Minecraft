using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Tells a client about a stack lying on the ground that has come within range of it. Separate from the
/// ordinary entity spawn because it carries what the stack is of, which no mob has to say — everything else
/// the tracker sends is identified by its type alone.
/// <para>
/// The wear travels with it so that a pickaxe thrown down with one swing left in it is still a pickaxe with
/// one swing left in it when somebody picks it up. Everything that is not a tool sends a zero.
/// </para>
/// </summary>
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
