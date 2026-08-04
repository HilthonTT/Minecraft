using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// Something growing out of the ground with no body of its own: a flower, a mushroom, a tuft of grass. It
/// cannot be walked into, and it uproots itself as soon as whatever was holding it up is taken away.
/// </summary>
/// <param name="growsOn">
/// The blocks this plant will stand on. Evaluated lazily, since the registry is still being filled in while
/// its blocks are being constructed.
/// </param>
public sealed class BlockPlant : Block
{
    private readonly Func<Block[]> _growsOnSelector;
    private Block[]? _growsOn;

    public BlockPlant(ushort id, Func<Block[]> growsOn) : base(id)
    {
        _growsOnSelector = growsOn;
        IsOpaque = false;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSimple(this);
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        // Drawn as two crossed quads that stop short of the edges of the cell, so the outline is pulled in to
        // match rather than framing a whole block of empty air.
        const float Inset = 0.25F;

        var min = new Vector3(blockPos.X + Inset, blockPos.Y, blockPos.Z + Inset);
        var max = new Vector3(
            blockPos.X + Constants.CUBE_DIM - Inset,
            blockPos.Y + Constants.CUBE_DIM - Inset,
            blockPos.Z + Constants.CUBE_DIM - Inset);

        return [new AxisAlignedBox(min, max)];
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block below = world.GetBlockAt(blockPos.Down()).GetBlock();
        return Array.IndexOf(_growsOn ??= _growsOnSelector(), below) >= 0;
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        if (blockPos == sourceBlockPos.Up() && world.GetBlockAt(sourceBlockPos).GetBlock() == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
