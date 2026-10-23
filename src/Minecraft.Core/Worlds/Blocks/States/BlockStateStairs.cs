using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateStairs(Block block) : BlockStateBits(block), IPlacementState
{
    private const int UpsideDownMask = 0b100;

    protected override byte ValidBits => FacingMask | UpsideDownMask;

    /// <summary>The side the bottom step is on. The tall half of the block is on the side opposite.</summary>
    public Direction Facing
    {
        get => HorizontalFacing;
        set => HorizontalFacing = value;
    }

    public bool IsUpsideDown
    {
        get => GetFlag(UpsideDownMask);
        set => SetFlag(UpsideDownMask, value);
    }

    public void ApplyPlacement(PlacementContext context)
    {
        Facing = DirectionUtil.FacingBack(context.LookDirection);
        IsUpsideDown = BlockStateSlab.IsUpperHalfPlacement(context);
    }

    public void CopyPlacementFrom(BlockState other)
    {
        if (other is BlockStateStairs stairs)
        {
            Facing = stairs.Facing;
            IsUpsideDown = stairs.IsUpsideDown;
        }
    }
}
