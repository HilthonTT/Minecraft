using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSugarCane : Block
{
    private const float SecondsToGrow = 1.0F;
    private const int MaxLength = 4;

    public BlockSugarCane(ushort id) : base(id)
    {
        IsTickable = true;
        IsOpaque = false;
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSugarCane();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer)
        {
            return;
        }

        var caneState = (BlockStateSugarCane)blockState;
        caneState.ElapsedTimeSinceLastGrowth += deltaTime;
        if (caneState.ElapsedTimeSinceLastGrowth < SecondsToGrow)
        {
            return;
        }

        caneState.ElapsedTimeSinceLastGrowth = 0;

        if (world.GetBlockAt(blockPos.Up()).GetBlock() == BlockRegistry.Air &&
            GetSugarCaneLength(world, blockPos) < MaxLength)
        {
            world.QueueToAddBlockAt(blockPos.Up(), GetNewDefaultState());
        }
    }

    private static int GetSugarCaneLength(World world, Vector3i blockPos)
    {
        int length = 1;
        while (world.GetBlockAt(blockPos.Down()).GetBlock() == BlockRegistry.SugarCane)
        {
            length++;
            blockPos = blockPos.Down();
        }
        return length;
    }

    public override void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnDestroy(blockState, world, blockPos);

        if (world is not WorldServer)
        {
            return;
        }

        while (world.GetBlockAt(blockPos.Up()).GetBlock() == BlockRegistry.SugarCane)
        {
            blockPos = blockPos.Up();
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block blockDown = world.GetBlockAt(blockPos.Down()).GetBlock();
        return blockDown == BlockRegistry.Sand ||
               blockDown == BlockRegistry.Grass ||
               blockDown == BlockRegistry.Dirt;
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

        if (sourceBlockPos != blockPos.Down() || world.GetBlockAt(sourceBlockPos).GetBlock() != BlockRegistry.Air)
        {
            return;
        }

        world.QueueToRemoveBlockAt(blockPos);

        // Cascade up the stack by hand: the block above is only notified once this one is actually gone,
        // which happens too late for it to see that its support disappeared.
        BlockState blockUp = world.GetBlockAt(blockPos.Up());
        blockUp?.GetBlock().OnNotify(blockUp, blockState, world, blockPos.Up(), blockPos);
    }
}
