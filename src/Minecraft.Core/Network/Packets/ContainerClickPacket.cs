using Minecraft.Core.Inventories;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class ContainerClickPacket : Packet
{
    public Vector3i BlockPos { get; private set; }

    public int Slot { get; private set; }

    public bool RightButton { get; private set; }

    public ItemStack Cursor { get; private set; }

    public int Sequence { get; private set; }

    public ContainerClickPacket(Vector3i blockPos, int slot, bool rightButton, ItemStack cursor, int sequence)
        : base(PacketType.ContainerClick)
    {
        BlockPos = blockPos;
        Slot = slot;
        RightButton = rightButton;
        Cursor = cursor;
        Sequence = sequence;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessContainerClickPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3i(BlockPos);
        bufferedStream.WriteInt32(Slot);
        bufferedStream.WriteBool(RightButton);
        ItemStackCodec.Write(bufferedStream, Cursor);
        bufferedStream.WriteInt32(Sequence);
    }
}
