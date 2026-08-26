using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Tells one player that they have just swept a stack up off the ground, and what it was.
/// <para>
/// The server owns the item lying in the world and decides who collected it; the inventory it lands in lives
/// on the client and nowhere else. So the world loses the item on the server and the client is told what it
/// now has, which is the one seam in the survival loop where the two sides hold different halves of the same
/// fact. Anything that will not fit is lost with it — thirty six slots is a great deal of room, and a server
/// side inventory is the change that would close it properly.
/// </para>
/// </summary>
public sealed class ItemPickupPacket : Packet
{
    public ushort ItemId { get; private set; }
    public int Count { get; private set; }

    /// <summary>How worn it is, so that a tool picked up is as worn as the one that was put down.</summary>
    public int Damage { get; private set; }

    /// <summary>The item this came from, so the client can drop it early rather than wait for the despawn.</summary>
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
