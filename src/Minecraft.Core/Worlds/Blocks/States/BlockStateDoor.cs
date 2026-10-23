using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateDoor(Block block) : BlockStateBits(block), IPlacementState
{
    private const int OpenMask = 0b100;
    private const int UpperMask = 0b1000;

    protected override byte ValidBits => FacingMask | OpenMask | UpperMask;

    /// <summary>The side of the cell the door sits against while it is shut, which is the side it was hung from.</summary>
    public Direction Facing
    {
        get => HorizontalFacing;
        set => HorizontalFacing = value;
    }

    public bool IsOpen
    {
        get => GetFlag(OpenMask);
        set => SetFlag(OpenMask, value);
    }

    public bool IsUpper
    {
        get => GetFlag(UpperMask);
        set => SetFlag(UpperMask, value);
    }

    /// <summary>
    /// The side the panel is against right now. Opening swings it a quarter turn about the hinge, which puts it
    /// against the neighbouring side.
    /// </summary>
    public Direction PanelSide => IsOpen ? ClockwiseOf(Facing) : Facing;

    public void ApplyPlacement(PlacementContext context)
    {
        Facing = DirectionUtil.FacingBack(context.LookDirection);
    }

    public void CopyPlacementFrom(BlockState other)
    {
        if (other is BlockStateDoor door)
        {
            Facing = door.Facing;
        }
    }

    private static Direction ClockwiseOf(Direction side) => side switch
    {
        Direction.Back => Direction.Right,
        Direction.Right => Direction.Front,
        Direction.Front => Direction.Left,
        _ => Direction.Back,
    };
}
