using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerEatPacket : Packet
{
    public ushort ItemId { get; private set; }

    public PlayerEatPacket(ushort itemId) : base(PacketType.PlayerEat)
    {
        ItemId = itemId;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerEatPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(ItemId);
    }
}
