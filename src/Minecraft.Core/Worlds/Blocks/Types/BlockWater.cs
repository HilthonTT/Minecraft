using Minecraft.Core.Inventories;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockWater : Block
{
    public const int MaxLevel = 7;

    private const float SourceSurfaceHeight = 0.875F;

    private const int FlowDelayTicks = 5;

    private static readonly Vector3i[] _sideOffsets =
    [
        Vector3iExtensions.NorthBasis,
        Vector3iExtensions.SouthBasis,
        Vector3iExtensions.EastBasis,
        Vector3iExtensions.WestBasis,
    ];

    public int Level { get; }

    public bool IsFalling { get; }

    public bool IsSource => Level == 0 && !IsFalling;

    public float SurfaceHeight { get; }

    private int FeedLevel => IsFalling ? 0 : Level;

    public BlockWater(ushort id, int level, bool falling) : base(id)
    {
        Level = level;
        IsFalling = falling;
        SurfaceHeight = falling
            ? Constants.CUBE_DIM
            : SourceSurfaceHeight * (MaxLevel + 1 - level) / (MaxLevel + 1);

        IsOpaque = false;

        IsOverridable = true;

        IsLiquid = true;
    }

    public static Block GetForLevel(int level)
    {
        return level switch
        {
            0 => BlockRegistry.Water,
            1 => BlockRegistry.WaterFlowing1,
            2 => BlockRegistry.WaterFlowing2,
            3 => BlockRegistry.WaterFlowing3,
            4 => BlockRegistry.WaterFlowing4,
            5 => BlockRegistry.WaterFlowing5,
            6 => BlockRegistry.WaterFlowing6,
            7 => BlockRegistry.WaterFlowing7,
            _ => BlockRegistry.Air,
        };
    }

    public override ItemStack GetDrop(BlockState blockState) => ItemStack.Empty;

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
        return _emptyAABB;
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnAdd(blockState, world, blockPos);
        world.ScheduleBlockUpdate(blockPos, FlowDelayTicks);
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        world.ScheduleBlockUpdate(blockPos, FlowDelayTicks);
    }

    public override void OnScheduledUpdate(BlockState blockState, World world, Vector3i blockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        if (!IsSource && !HoldsItsLevel(world, blockPos))
        {
            return;
        }

        Spread(world, blockPos);
    }

    private bool HoldsItsLevel(World world, Vector3i blockPos)
    {
        int springsBeside = 0;
        int shallowestFeed = int.MaxValue;

        foreach (Vector3i sideOffset in _sideOffsets)
        {
            Vector3i sidePos = blockPos + sideOffset;

            if (!world.IsBlockPositionLoaded(sidePos))
            {
                return true;
            }

            if (world.GetBlockAt(sidePos).GetBlock() is not BlockWater beside)
            {
                continue;
            }

            if (beside.IsSource)
            {
                springsBeside++;
            }

            shallowestFeed = Math.Min(shallowestFeed, beside.FeedLevel);
        }

        bool fedFromAbove = world.GetBlockAt(blockPos.Up()).GetBlock() is BlockWater;

        Block wanted;
        if (fedFromAbove)
        {
            wanted = BlockRegistry.WaterFalling;
        }
        else if (springsBeside >= 2)
        {
            wanted = BlockRegistry.Water;
        }
        else if (shallowestFeed == int.MaxValue)
        {
            wanted = BlockRegistry.Air;
        }
        else
        {
            wanted = GetForLevel(shallowestFeed + 1);
        }

        if (wanted == this)
        {
            return true;
        }

        if (wanted == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
        else
        {
            world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(wanted));
        }

        return false;
    }

    private void Spread(World world, Vector3i blockPos)
    {
        Vector3i belowPos = blockPos.Down();
        if (CanFlowInto(world, belowPos))
        {
            FlowInto(world, belowPos, BlockRegistry.WaterFalling);
            return;
        }

        if (GetForLevel(FeedLevel + 1) is not BlockWater thinner)
        {
            return;
        }

        foreach (Vector3i sideOffset in _sideOffsets)
        {
            Vector3i sidePos = blockPos + sideOffset;

            if (CanFlowInto(world, sidePos))
            {
                FlowInto(world, sidePos, thinner);
                continue;
            }

            if (world.GetBlockAt(sidePos).GetBlock() is BlockWater beside &&
                !beside.IsSource &&
                !beside.IsFalling &&
                beside.Level > thinner.Level)
            {
                FlowInto(world, sidePos, thinner);
            }
        }
    }

    private static bool CanFlowInto(World world, Vector3i blockPos)
    {
        if (!world.IsBlockPositionLoaded(blockPos))
        {
            return false;
        }

        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        if (block == BlockRegistry.Air)
        {
            return true;
        }

        if (block is BlockWater)
        {
            return false;
        }

        return block.GetCollisionBox(state, blockPos).Length == 0;
    }

    private static void FlowInto(World world, Vector3i blockPos, Block water)
    {
        if (!world.GetBlockAt(blockPos).GetBlock().IsOverridable)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }

        world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(water));
    }
}
