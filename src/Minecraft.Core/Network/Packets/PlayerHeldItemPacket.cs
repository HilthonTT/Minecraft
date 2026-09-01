using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerHeldItemPacket : Packet
{
    public ushort ItemId { get; private set; }

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
