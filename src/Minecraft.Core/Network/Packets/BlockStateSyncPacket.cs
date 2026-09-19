using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class BlockStateSyncPacket : Packet
{
    public Vector3i BlockPos { get; private set; }

    public int AcknowledgedSequence { get; private set; }

    public BlockState BlockState { get; private set; }

    public BlockStateSyncPacket(Vector3i blockPos, int acknowledgedSequence, BlockState blockState)
        : base(PacketType.BlockStateSync)
    {
        BlockPos = blockPos;
        AcknowledgedSequence = acknowledgedSequence;
        BlockState = blockState;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessBlockStateSyncPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3i(BlockPos);
        bufferedStream.WriteInt32(AcknowledgedSequence);
        bufferedStream.WriteInt32(BlockState.PayloadSize());
        BlockState.ToStream(bufferedStream);
    }
}
