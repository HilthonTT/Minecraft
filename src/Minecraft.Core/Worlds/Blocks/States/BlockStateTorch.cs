using Minecraft.Core.IO;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

/// <summary>
/// A torch, and which way it is attached. The only light the player can put down themselves, and warmer than
/// the glowstone the world generates with, so a lit cave reads as having been lit by somebody.
/// </summary>
public sealed class BlockStateTorch : BlockState, ILightSource, IOrientedBlockState
{
    /// <summary>
    /// Where the block holding this torch up is, seen from the torch. <see cref="Direction.Bottom"/> is a
    /// torch standing on the ground; anything horizontal is one leaning off a wall.
    /// </summary>
    public Direction Attachment { get; set; } = Direction.Bottom;

    /// <summary>Whether the torch is leaning off a wall rather than standing on the ground.</summary>
    public bool IsOnWall => Attachment != Direction.Bottom;

    /// <summary>
    /// Deep orange. Short of glowstone's reach on every channel, and much shorter on blue, so a torch lights
    /// a smaller room than a glowstone seam does and lights it the colour of a flame rather than of daylight.
    /// </summary>
    public Vector3i LightColor { get; } = new(14, 9, 3);

    public override Block GetBlock()
    {
        return BlockRegistry.Torch;
    }

    public void OrientTowardsSupport(Vector3i offsetToSupport)
    {
        // A torch cannot hang off a ceiling, and a zero offset means it replaced what it was placed against
        // rather than sitting beside it. Both leave it standing on whatever is underneath.
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

        // A torch on the ceiling is not a shape this block has, so anything unreadable stands it back up
        // rather than leaving the model and the support test disagreeing about where it is.
        Attachment = stored is Direction.Top || !Enum.IsDefined(stored) ? Direction.Bottom : stored;
    }
}
