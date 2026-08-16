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
    public ushort BlockId { get; private set; }
    public int Count { get; private set; }

    public PlayerDropItemPacket(ushort blockId, int count) : base(PacketType.PlayerDropItem)
    {
        BlockId = blockId;
        Count = count;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerDropItemPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(BlockId);
        bufferedStream.WriteInt32(Count);
    }
}
