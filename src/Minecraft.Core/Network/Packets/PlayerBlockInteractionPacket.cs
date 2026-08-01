using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Utilities.Vectors;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerBlockInteractionPacket : Packet
{
    public Vector3i BlockPos { get; private set; }

    public PlayerBlockInteractionPacket(Vector3i blockPos) : base(PacketType.PlayerBlockInteraction)
    {
        BlockPos = blockPos;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerBlockInteractionpacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3i(BlockPos);
    }
}
