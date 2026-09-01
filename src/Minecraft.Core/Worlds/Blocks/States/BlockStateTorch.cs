using Minecraft.Core.IO;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateTorch : BlockState, ILightSource, IOrientedBlockState
{
    public Direction Attachment { get; set; } = Direction.Bottom;

    public bool IsOnWall => Attachment != Direction.Bottom;

    public Vector3i LightColor { get; } = new(14, 9, 3);

    public override Block GetBlock()
    {
        return BlockRegistry.Torch;
    }

    public void OrientTowardsSupport(Vector3i offsetToSupport)
    {
        Attachment = offsetToSupport switch
        {
            { X: 0, Y: -1, Z: 0 } => Direction.Bottom,
            { X: -1, Y: 0, Z: 0 } => Direction.Left,
            { X: 1, Y: 0, Z: 0 } => Direction.Right,
            { X: 0, Y: 0, Z: -1 } => Direction.Back,
            { X: 0, Y: 0, Z: 1 } => Direction.Front,
            _ => Direction.Bottom,
        };
    }

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteByte((byte)Attachment);
    }

    public override int PayloadSize() => sizeof(byte);

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        Direction stored = DataConverter.BytesToByteStruct<Direction>(bytes, ref head);

        Attachment = stored is Direction.Top || !Enum.IsDefined(stored) ? Direction.Bottom : stored;
    }
}
