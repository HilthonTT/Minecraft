using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGrassBlade : Block
{
    public BlockGrassBlade(ushort id) : base(id)
    {
        IsOpaque = false;
        IsOverridable = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGrassBlade();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block block = world.GetBlockAt(blockPos.Down()).GetBlock();
        return block == BlockRegistry.Dirt || block == BlockRegistry.Grass;
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
