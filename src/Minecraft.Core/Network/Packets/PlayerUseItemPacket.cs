using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

/// <summary>A right click with a hoe or bucket at a block.</summary>
public sealed class PlayerUseItemPacket : Packet
{
    public ushort ItemId { get; private set; }

    public Vector3i Target { get; private set; }

    public PlayerUseItemPacket(ushort itemId, Vector3i target) : base(PacketType.PlayerUseItem)
    {
        ItemId = itemId;
        Target = target;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerUseItemPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(ItemId);
        bufferedStream.WriteVector3i(Target);
    }
}
