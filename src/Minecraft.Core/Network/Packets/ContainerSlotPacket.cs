using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class ContainerSlotPacket : Packet
{
    public Vector3i BlockPos { get; private set; }

    public int Slot { get; private set; }

    public ushort ItemId { get; private set; }

    public int Count { get; private set; }

    public int Damage { get; private set; }

    public int Sequence { get; private set; }

    public ContainerSlotPacket(Vector3i blockPos, int slot, ushort itemId, int count, int damage, int sequence)
        : base(PacketType.ContainerSlot)
    {
        BlockPos = blockPos;
        Slot = slot;
        ItemId = itemId;
        Count = count;
        Damage = damage;
        Sequence = sequence;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessContainerSlotPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3i(BlockPos);
        bufferedStream.WriteInt32(Slot);
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteInt32(Count);
        bufferedStream.WriteInt32(Damage);
        bufferedStream.WriteInt32(Sequence);
    }
}
