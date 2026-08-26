using Minecraft.Core.Inventories;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// Water, still or running. It fills its cell but stops nothing: there is no collision box, so an entity
/// swims through it rather than standing on it, and no selection box, so a click aimed through it reaches
/// the ground underneath instead of hitting the water.
/// </summary>
/// <remarks>
/// How deep the water stands in a cell is carried by which block it is rather than by a state on the side.
/// A sea is millions of cells of water and a section stores a bare id per cell, keeping a full state only
/// for the handful of blocks that need one, so a level held as state would put an object and a dictionary
/// entry behind every cell of every ocean in view. A block per level costs eight registry entries once.
/// <para>
/// A source never empties, which is what makes a lake a lake: breaking its wall floods what is beyond it
/// rather than draining what is behind. Running water is the opposite and holds nothing of its own — it is
/// worked out afresh from what feeds it every time it is looked at, and dries up the moment nothing does.
/// </para>
/// </remarks>
public sealed class BlockWater : Block
{
    /// <summary>
    /// The thinnest running water there is, and so how far from its source a flow reaches across flat
    /// ground: each cell feeds the next one level thinner, and past this there is nothing left to give.
    /// </summary>
    public const int MaxLevel = 7;

    /// <summary>How deep a source stands within its cell. Short of the top, so a surface reads as one.</summary>
    private const float SourceSurfaceHeight = 0.875F;

    /// <summary>
    /// How long water waits before it moves again, in ticks. Slow enough that a flow spreading out reads as
    /// water running rather than as a sheet appearing at once.
    /// </summary>
    private const int FlowDelayTicks = 5;

    /// <summary>The four ways water can run sideways.</summary>
    private static readonly Vector3i[] _sideOffsets =
    [
        Vector3iExtensions.NorthBasis,
        Vector3iExtensions.SouthBasis,
        Vector3iExtensions.EastBasis,
        Vector3iExtensions.WestBasis,
    ];

    /// <summary>How thin this water is: zero at a source, rising to <see cref="MaxLevel"/> as it runs out.</summary>
    public int Level { get; }

    /// <summary>
    /// Whether this is water on its way down a drop. It fills its cell rather than standing at a level, and
    /// feeds what is around the bottom of the fall as strongly as a source would.
    /// </summary>
    public bool IsFalling { get; }

    /// <summary>Whether this water is a spring that never empties, as opposed to a flow that can dry up.</summary>
    public bool IsSource => Level == 0 && !IsFalling;

    /// <summary>Where the top of the water sits within its cell, as a fraction of it.</summary>
    public float SurfaceHeight { get; }

    /// <summary>
    /// The level this water feeds its neighbours from. Water coming down a drop hands on what a source would,
    /// so a fall reaching the floor spreads out from there rather than arriving already spent.
    /// </summary>
    private int FeedLevel => IsFalling ? 0 : Level;

    public BlockWater(ushort id, int level, bool falling) : base(id)
    {
        Level = level;
        IsFalling = falling;
        SurfaceHeight = falling
            ? Constants.CUBE_DIM
            : SourceSurfaceHeight * (MaxLevel + 1 - level) / (MaxLevel + 1);

        // Light passes through, so a shallow seabed stays lit rather than sitting in the dark under a lid.
        IsOpaque = false;

        // What lets a block be placed into water: the placement lands on the water cell and takes it over,
        // the same way it would an empty one.
        IsOverridable = true;

        IsLiquid = true;
    }

    /// <summary>
    /// The water that stands at the given level, or air past the point where a flow has run out. Written as
    /// a lookup rather than held in an array because the registry is still filling itself in while its
    /// blocks are being built, and an array would be read before it was there.
    /// </summary>
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

    /// <summary>
    /// Water is never broken — it has no selection box, so nothing can be aimed at it, and it only ever
    /// leaves a cell by being displaced. It therefore drops nothing, which is what stops a bucketless player
    /// from carrying an ocean home a cell at a time should anything ever manage to aim at one.
    /// </summary>
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
        // Anything changing next to water can open a way for it to run or take away what was feeding it.
        // Which of the two it was is not worked out here, since the answer depends on all six sides at once
        // and every one of them is read when the update comes round.
        world.ScheduleBlockUpdate(blockPos, FlowDelayTicks);
    }

    public override void OnScheduledUpdate(BlockState blockState, World world, Vector3i blockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        // Running water that no longer belongs at this level is replaced rather than corrected in place, and
        // what replaces it asks to be looked at in turn. Nothing is spread from a cell that is on its way
        // out, so a flow being cut off retreats to its source instead of feeding itself along the way.
        if (!IsSource && !HoldsItsLevel(world, blockPos))
        {
            return;
        }

        Spread(world, blockPos);
    }

    /// <summary>
    /// Works out what should be standing at this position now and returns whether that is what already is.
    /// Water fed from above is a fall, water pooled between two springs becomes one itself, and water with
    /// nothing left feeding it is taken away.
    /// </summary>
    private bool HoldsItsLevel(World world, Vector3i blockPos)
    {
        int springsBeside = 0;
        int shallowestFeed = int.MaxValue;

        foreach (Vector3i sideOffset in _sideOffsets)
        {
            Vector3i sidePos = blockPos + sideOffset;

            // A side that is not loaded reads as air, which would look like a feed drying up. Water at the
            // edge of the loaded world is left exactly as it is rather than judged on what cannot be seen.
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
            // Water lying between two springs becomes one, which is what makes a pool dug between a pair of
            // them worth drawing from: it fills itself back in as fast as it is taken away.
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

    /// <summary>
    /// Runs this water into whatever is open around it. Downwards first, and sideways only once there is
    /// something under it to hold it up, which is what makes a drop a column of water rather than a spray.
    /// <para>
    /// Water already standing below counts as being held up rather than as somewhere still to fall: a pool
    /// being filled from above has to spread out across its own surface, or it would only ever be one cell
    /// wide however much was poured into it.
    /// </para>
    /// </summary>
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
            // Spent: this cell is the far edge of the flow and has nothing left to hand on.
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

            // Water already there, but thinner than what this cell feeds it. Topping it up is what lets a
            // flow that found a shorter way round afterwards deepen the long way it took first.
            if (world.GetBlockAt(sidePos).GetBlock() is BlockWater beside &&
                !beside.IsSource &&
                !beside.IsFalling &&
                beside.Level > thinner.Level)
            {
                FlowInto(world, sidePos, thinner);
            }
        }
    }

    /// <summary>
    /// Whether water can take over the given cell. Anything growing out of the ground gives way and is
    /// washed out; anything that holds a body up holds water back too. Water itself is left to the caller,
    /// which has to weigh what is already there against what it would be replaced with.
    /// </summary>
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
        // Whatever was standing here is washed out first. A placement may only take over a cell whose block
        // agreed to give it up, and a flower has not, so it has to be taken away rather than built over.
        if (!world.GetBlockAt(blockPos).GetBlock().IsOverridable)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }

        world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(water));
    }
}
