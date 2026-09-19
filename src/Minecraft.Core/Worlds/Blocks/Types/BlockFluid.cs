using Minecraft.Core.Inventories;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public abstract class BlockFluid : Block
{
    private const float SourceSurfaceHeight = 0.875F;

    protected static readonly Vector3i[] _sideOffsets =
    [
        Vector3iExtensions.NorthBasis,
        Vector3iExtensions.SouthBasis,
        Vector3iExtensions.EastBasis,
        Vector3iExtensions.WestBasis,
    ];

    public int Level { get; }

    public int MaxLevel { get; }

    public bool IsFalling { get; }

    public bool IsSource => Level == 0 && !IsFalling;

    public float SurfaceHeight { get; }

    private int FeedLevel => IsFalling ? 0 : Level;

    protected BlockFluid(ushort id, int level, int maxLevel, bool falling) : base(id)
    {
        Level = level;
        MaxLevel = maxLevel;
        IsFalling = falling;
        SurfaceHeight = falling
            ? Constants.CUBE_DIM
            : SourceSurfaceHeight * (maxLevel + 1 - level) / (maxLevel + 1);

        IsOpaque = false;

        IsOverridable = true;

        IsLiquid = true;
    }

    protected abstract int FlowDelayTicks { get; }

    protected abstract bool FormsSourcesBetweenSprings { get; }

    protected abstract Block FallingBlock { get; }

    public abstract Block GetForLevel(int level);

    public bool IsSameFluidAs(Block other) => other is BlockFluid && other.GetType() == GetType();

    protected virtual bool TryReactWithNeighbours(World world, Vector3i blockPos) => false;

    protected virtual void FlowIntoOtherFluid(World world, Vector3i blockPos)
    {
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

        if (TryReactWithNeighbours(world, blockPos))
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

            Block side = world.GetBlockAt(sidePos).GetBlock();
            if (!IsSameFluidAs(side))
            {
                continue;
            }

            var beside = (BlockFluid)side;

            if (beside.IsSource)
            {
                springsBeside++;
            }

            shallowestFeed = Math.Min(shallowestFeed, beside.FeedLevel);
        }

        bool fedFromAbove = IsSameFluidAs(world.GetBlockAt(blockPos.Up()).GetBlock());

        Block wanted;
        if (fedFromAbove)
        {
            wanted = FallingBlock;
        }
        else if (FormsSourcesBetweenSprings && springsBeside >= 2)
        {
            wanted = GetForLevel(0);
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
            FlowInto(world, belowPos, FallingBlock);
            return;
        }

        if (IsOtherFluidAt(world, belowPos))
        {
            FlowIntoOtherFluid(world, belowPos);
            return;
        }

        if (GetForLevel(FeedLevel + 1) is not BlockFluid thinner)
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

            if (IsOtherFluidAt(world, sidePos))
            {
                FlowIntoOtherFluid(world, sidePos);
                continue;
            }

            Block side = world.GetBlockAt(sidePos).GetBlock();
            if (IsSameFluidAs(side) &&
                side is BlockFluid beside &&
                !beside.IsSource &&
                !beside.IsFalling &&
                beside.Level > thinner.Level)
            {
                FlowInto(world, sidePos, thinner);
            }
        }
    }

    private bool IsOtherFluidAt(World world, Vector3i blockPos)
    {
        if (!world.IsBlockPositionLoaded(blockPos))
        {
            return false;
        }

        Block block = world.GetBlockAt(blockPos).GetBlock();
        return block is BlockFluid && !IsSameFluidAs(block);
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

        if (block is BlockFluid)
        {
            return false;
        }

        return block.GetCollisionBox(state, blockPos).Length == 0;
    }

    private static void FlowInto(World world, Vector3i blockPos, Block fluid)
    {
        if (!world.GetBlockAt(blockPos).GetBlock().IsOverridable)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }

        world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(fluid));
    }
}
