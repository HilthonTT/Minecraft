using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

/// <summary>
/// A fence or pane, and which of its four sides it reaches across to a neighbour. That is worked out from the
/// neighbours whenever one of them changes rather than when the block is drawn, because the shape it gives is
/// also what a player walks into, and collision is asked without a world to look the neighbours up in.
/// </summary>
public sealed class BlockStateConnecting(Block block) : BlockStateBits(block)
{
    protected override byte ValidBits => 0b1111;

    public bool ConnectsTowards(Direction side) => GetFlag(MaskFor(side));

    public bool HasNoConnections => _bits == 0;

    /// <returns>Whether anything changed.</returns>
    public bool SetConnections(Func<Direction, bool> connectsTowards)
    {
        byte before = _bits;

        foreach (Direction side in Sides)
        {
            SetFlag(MaskFor(side), connectsTowards(side));
        }

        return _bits != before;
    }

    public static readonly Direction[] Sides = [Direction.Back, Direction.Right, Direction.Front, Direction.Left];

    private static int MaskFor(Direction side) => 1 << (int)side;
}
