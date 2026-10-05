using Minecraft.Core.Inventories;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class ContainerCursorPacket : Packet
{
    public int Sequence { get; private set; }

    public ItemStack Cursor { get; private set; }

    public ContainerCursorPacket(int sequence, ItemStack cursor) : base(PacketType.ContainerCursor)
    {
        Sequence = sequence;
        Cursor = cursor;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessContainerCursorPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(Sequence);
        ItemStackCodec.Write(bufferedStream, Cursor);
    }
}
