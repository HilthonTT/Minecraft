using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// A player throwing down what they were holding.
/// <para>
/// The stack travels with the request because the inventory it came out of lives on the client and nowhere
/// else — the server has no copy of it to take anything from, and so has to be told what was thrown. That is
/// the same seam <see cref="ItemPickupPacket"/> sits on, read from the other end: one says what a player now
/// has, and this says what they have just stopped having.
/// </para>
/// </summary>
public sealed class PlayerDropItemPacket : Packet
{
    public ushort ItemId { get; private set; }
    public int Count { get; private set; }

    /// <summary>How worn it was, so that throwing a tool down and picking it up again does not mend it.</summary>
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
