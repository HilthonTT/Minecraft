namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateSlab(Block block) : BlockStateBits(block), IPlacementState
{
    private const int TopMask = 0b1;

    protected override byte ValidBits => TopMask;

    public bool IsTop
    {
        get => GetFlag(TopMask);
        set => SetFlag(TopMask, value);
    }

    public void ApplyPlacement(PlacementContext context)
    {
        IsTop = IsUpperHalfPlacement(context);
    }

    public void CopyPlacementFrom(BlockState other)
    {
        if (other is BlockStateSlab slab)
        {
            IsTop = slab.IsTop;
        }
    }

    /// <summary>
    /// Clicking the underside of a block hangs a slab from it, clicking a top rests one on it, and clicking a side
    /// puts it in whichever half of the side was clicked.
    /// </summary>
    public static bool IsUpperHalfPlacement(PlacementContext context) =>
        context.ClickedUnderside || (!context.ClickedTopside && context.HitHeightInCell >= 0.5F);
}
