using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// A player saying what they are now holding, sent whenever the selection moves onto something different.
/// <para>
/// The server has no copy of anybody's inventory, and until tools existed it never needed one: a block broken
/// left the same thing behind whatever had been swung at it. A tool changes that — what falls out of a seam
/// of iron depends on what it was struck with — so the one fact the server does need is sent to it, and only
/// when it changes rather than with every swing.
/// </para>
/// <para>
/// This is the third strand of the same seam <see cref="ItemPickupPacket"/> and
/// <see cref="PlayerDropItemPacket"/> sit on, and it is trusted the same way they are: a client that lied here
/// would be claiming a better pickaxe than it owns. Closing that means an inventory the server keeps, which
/// is the change all three of these are waiting on.
/// </para>
/// </summary>
public sealed class PlayerHeldItemPacket : Packet
{
    /// <summary>The item in hand, or zero for an empty one. Zero is no item's id.</summary>
    public ushort ItemId { get; private set; }

    /// <summary>How worn it is. Sent so the server can wear it through and stop honouring it.</summary>
    public int Damage { get; private set; }

    public PlayerHeldItemPacket(ushort itemId, int damage) : base(PacketType.PlayerHeldItem)
    {
        ItemId = itemId;
        Damage = damage;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerHeldItemPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteInt32(Damage);
    }
}
