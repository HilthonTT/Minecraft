using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A torch: a light the player can carry into the dark and put down. It stands on the ground or leans off a
/// wall, has no body to walk into, and falls the moment whatever was holding it up is taken away.
/// </summary>
public sealed class BlockTorch : Block
{
    /// <summary>How far in from the sides of its cell the torch's outline is drawn.</summary>
    private const float SelectionInset = 0.36F;

    /// <summary>How tall the stick is, as a share of a block. The flame sits just above it.</summary>
    private const float StickHeight = 0.65F;

    public BlockTorch(ushort id) : base(id)
    {
        IsOpaque = false;
        SoundMaterial = BlockSoundMaterial.Wood;

        // Every torch remembers which way it was put down, so no two of them can share one state.
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateTorch();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        // The outline follows the stick rather than the cell it stands in, which would otherwise frame a
        // whole block of empty air around something the width of a finger.
        var min = new Vector3(blockPos.X + SelectionInset, blockPos.Y, blockPos.Z + SelectionInset);
        var max = new Vector3(
            blockPos.X + Constants.CUBE_DIM - SelectionInset,
            blockPos.Y + StickHeight,
            blockPos.Z + Constants.CUBE_DIM - SelectionInset);

        if (state is BlockStateTorch torch && torch.IsOnWall)
        {
            // A wall torch is carried up its wall and pushed back against it, so the outline sits where the
            // stick actually is instead of hanging in the middle of the cell.
            Vector3i towardsWall = DirectionUtil.ToUnit(torch.Attachment);
            var shift = new Vector3(towardsWall.X * 0.3F, 0.2F, towardsWall.Z * 0.3F);
            min += shift;
            max += shift;
        }

        return [new AxisAlignedBox(min, max)];
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        // Anything the torch could be put on will do here. Which of them it is actually attached to is
        // decided when it is placed, from the face that was clicked.
        if (IsSupporting(world, blockPos.Down()))
        {
            return true;
        }

        foreach (Direction side in _wallSides)
        {
            if (IsSupporting(world, blockPos + DirectionUtil.ToUnit(side)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Drops the torch when the block holding it up goes. Only the attachment matters: a torch on a wall is
    /// unaffected by the floor beneath it being dug out, and one on the floor by the wall behind it.
    /// </summary>
    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer || blockState is not BlockStateTorch torch)
        {
            return;
        }

        Vector3i supportPos = blockPos + DirectionUtil.ToUnit(torch.Attachment);
        if (sourceBlockPos == supportPos && !IsSupporting(world, supportPos))
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    /// <summary>The four sides a torch may lean off, which is every direction but up and down.</summary>
    private static readonly Direction[] _wallSides =
    [
        Direction.Back,
        Direction.Right,
        Direction.Front,
        Direction.Left,
    ];

    /// <summary>
    /// Whether the block at the given position can hold a torch. Anything that fills its cell will: leaves
    /// and glass do not, and neither does another torch, which is what stops a stack of them climbing into
    /// the air.
    /// </summary>
    private static bool IsSupporting(World world, Vector3i blockPos)
    {
        return !world.IsOutsideBuildHeight(blockPos.Y) && world.GetBlockAt(blockPos).GetBlock().IsOpaque;
    }
}
