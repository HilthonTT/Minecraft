using Minecraft.Core.Utilities.Spatial;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateLadder(Block block) : BlockStateBits(block), IPlacementState
{
    protected override byte ValidBits => FacingMask;

    /// <summary>The side of the cell the wall holding the ladder is on.</summary>
    public Direction Attachment
    {
        get => HorizontalFacing;
        set => HorizontalFacing = value;
    }

    public void ApplyPlacement(PlacementContext context)
    {
        Vector3i towardsClicked = context.ClickedBlockPos - context.BlockPos;

        foreach (Direction side in DirectionsWorthTrying(towardsClicked))
        {
            if (Types.BlockLadder.IsWall(context.World, context.BlockPos + DirectionUtil.ToUnit(side)))
            {
                Attachment = side;
                return;
            }
        }
    }

    public void CopyPlacementFrom(BlockState other)
    {
        if (other is BlockStateLadder ladder)
        {
            Attachment = ladder.Attachment;
        }
    }

    // The clicked wall first, since that is the one that was aimed at; then any wall, for a click on a floor.
    private static IEnumerable<Direction> DirectionsWorthTrying(Vector3i towardsClicked)
    {
        foreach (Direction side in Types.BlockLadder.WallSides)
        {
            if (DirectionUtil.ToUnit(side) == towardsClicked)
            {
                yield return side;
            }
        }

        foreach (Direction side in Types.BlockLadder.WallSides)
        {
            yield return side;
        }
    }
}
