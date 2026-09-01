using Minecraft.Core.Utilities.Vectors;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public abstract class BlockFalling : Block
{
    private const int FallDelayTicks = 1;

    protected BlockFalling(ushort id) : base(id)
    {
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnAdd(blockState, world, blockPos);

        world.ScheduleBlockUpdate(blockPos, FallDelayTicks);
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (sourceBlockPos == blockPos.Down())
        {
            world.ScheduleBlockUpdate(blockPos, FallDelayTicks);
        }
    }

    public override void OnScheduledUpdate(BlockState blockState, World world, Vector3i blockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        Vector3i belowPos = blockPos.Down();
        if (!world.IsBlockPositionLoaded(belowPos) || !IsNothingToRestOn(world, belowPos))
        {
            return;
        }

        BlockState landing = BlockRegistry.GetState(this);
        if (world.IsBlockedByEntity(belowPos, landing))
        {
            world.ScheduleBlockUpdate(blockPos, FallDelayTicks);
            return;
        }

        if (!world.GetBlockAt(belowPos).GetBlock().IsOverridable)
        {
            world.QueueToRemoveBlockAt(belowPos);
        }

        world.QueueToRemoveBlockAt(blockPos);
        world.QueueToAddBlockAt(belowPos, landing);
    }

    private static bool IsNothingToRestOn(World world, Vector3i blockPos)
    {
        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        return block == BlockRegistry.Air || block.GetCollisionBox(state, blockPos).Length == 0;
    }
}
