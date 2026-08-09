using Minecraft.Core.Utilities.Vectors;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A block with nothing holding it together, which drops as soon as what it was resting on is taken away.
/// Digging out the bottom of a bank of sand brings the rest of it down, and a pillar of gravel left standing
/// in mid air falls the moment it is touched.
/// </summary>
/// <remarks>
/// The block moves a cell at a time rather than turning into a falling body of its own. A fall is therefore
/// a run of ordinary block changes, which everything downstream already knows what to do with: the clients
/// watching are told about it the same way they are told about a block being placed, and a pile that came
/// down while nobody was near is on disk as the blocks it settled into rather than as something in flight.
/// </remarks>
public abstract class BlockFalling : Block
{
    /// <summary>
    /// How long the block waits between the cells it drops through, in ticks. One, so that a fall is about
    /// as quick as gravity would carry it and a tall column does not hang in the air on the way down.
    /// </summary>
    private const int FallDelayTicks = 1;

    protected BlockFalling(ushort id) : base(id)
    {
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnAdd(blockState, world, blockPos);

        // Asked for on every placement, including the ones a fall itself makes, which is what carries the
        // block down cell after cell until something stops it.
        world.ScheduleBlockUpdate(blockPos, FallDelayTicks);
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        // Only what is underneath can start a fall. A block appearing or going away beside this one changes
        // nothing about whether it is being held up.
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

        // Somebody standing in the way holds the fall up where it is rather than being buried, since a
        // placement into an entity is refused and the block would then have gone from the world altogether.
        // Asked again rather than given up on, so that the drop carries on the moment they step aside.
        BlockState landing = BlockRegistry.GetState(this);
        if (world.IsBlockedByEntity(belowPos, landing))
        {
            world.ScheduleBlockUpdate(blockPos, FallDelayTicks);
            return;
        }

        // Whatever was in the way goes with it: a fall lands on flowers and shallow water rather than
        // stopping on them. Removals are carried out before placements, so the cell is clear by the time
        // the block arrives in it.
        if (!world.GetBlockAt(belowPos).GetBlock().IsOverridable)
        {
            world.QueueToRemoveBlockAt(belowPos);
        }

        world.QueueToRemoveBlockAt(blockPos);
        world.QueueToAddBlockAt(belowPos, landing);
    }

    /// <summary>Whether the given cell offers nothing that would hold this block up.</summary>
    private static bool IsNothingToRestOn(World world, Vector3i blockPos)
    {
        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        return block == BlockRegistry.Air || block.GetCollisionBox(state, blockPos).Length == 0;
    }
}
