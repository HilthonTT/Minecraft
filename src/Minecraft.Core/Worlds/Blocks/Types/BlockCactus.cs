using Minecraft.Core.Audio;
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
        SoundMaterial = BlockSoundMaterial.Cloth;
        SecondsToBreak = 0.4F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateCactus();
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block blockDown = world.GetBlockAt(blockPos.Down()).GetBlock();
        if (blockDown != BlockRegistry.Sand && blockDown != BlockRegistry.Cactus)
        {
            return false;
        }

        return !HasSolidBlockAt(world, blockPos.North()) &&
               !HasSolidBlockAt(world, blockPos.South()) &&
               !HasSolidBlockAt(world, blockPos.East()) &&
               !HasSolidBlockAt(world, blockPos.West());
    }

    private static bool HasSolidBlockAt(World world, Vector3i blockPos)
    {
        BlockState blockState = world.GetBlockAt(blockPos);
        return blockState.GetBlock().GetCollisionBox(blockState, blockPos).Length > 0;
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

        while (world.GetBlockAt(blockPos.Up()).GetBlock() == BlockRegistry.Cactus)
        {
            blockPos = blockPos.Up();
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
