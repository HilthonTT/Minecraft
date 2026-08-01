using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockCactus : Block
{
    public BlockCactus(ushort id) : base(id)
    {
        IsOpaque = false;
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateCactus();
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        return world.GetBlockAt(blockPos.Down()).GetBlock() == BlockRegistry.Sand;
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

    public override void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnDestroy(blockState, world, blockPos);

        if (world is not WorldServer)
        {
            return;
        }

        // A cactus is a single stack, so removing any segment brings down everything above it.
        while (world.GetBlockAt(blockPos.Up()).GetBlock() == BlockRegistry.Cactus)
        {
            blockPos = blockPos.Up();
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
