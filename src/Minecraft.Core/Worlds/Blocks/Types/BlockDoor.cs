using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A door is two cells, each its own block state, kept in step: the lower one is what gets placed, it hangs the
/// upper one above itself, and breaking or opening either does the same to the other.
/// </summary>
public sealed class BlockDoor : Block
{
    public BlockDoor(ushort id) : base(id)
    {
        IsOpaque = false;
        IsInteractable = true;
        HasCustomState = true;
        IsPlacedByItsOwnItem = false;
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 3.0F;
        HarvestTool = ToolKind.Axe;
    }

    public override BlockState GetNewDefaultState() => new BlockStateDoor(this);

    public override ItemStack GetDrop(BlockState blockState) => new(ItemRegistry.Door, 1);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos) =>
        state is BlockStateDoor door ? BlockShapes.InWorld(BlockShapes.Door(door.PanelSide), blockPos) : _emptyAABB;

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos) =>
        GetCollisionBox(state, blockPos);

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Vector3i above = blockPos.Up();
        if (world.IsOutsideBuildHeight(blockPos.Y - 1) || world.IsOutsideBuildHeight(above.Y))
        {
            return false;
        }

        Block aboveBlock = world.GetBlockAt(above).GetBlock();

        return world.GetBlockAt(blockPos.Down()).GetBlock().IsOpaque &&
               (aboveBlock == BlockRegistry.Air || (aboveBlock.IsOverridable && !aboveBlock.IsLiquid));
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnAdd(blockState, world, blockPos);

        if (world is not WorldServer || blockState is not BlockStateDoor { IsUpper: false } lower)
        {
            return;
        }

        var upper = new BlockStateDoor(this) { Facing = lower.Facing, IsOpen = lower.IsOpen, IsUpper = true };
        world.QueueToAddBlockAt(blockPos.Up(), upper);
    }

    public override void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        base.OnDestroy(blockState, world, blockPos);

        if (world is not WorldServer || blockState is not BlockStateDoor door)
        {
            return;
        }

        Vector3i otherHalf = OtherHalfOf(door, blockPos);
        if (world.GetBlockAt(otherHalf).GetBlock() == this)
        {
            world.QueueToRemoveBlockAt(otherHalf);
        }
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer || blockState is not BlockStateDoor { IsUpper: false })
        {
            return;
        }

        if (sourceBlockPos == blockPos.Down() && !world.GetBlockAt(sourceBlockPos).GetBlock().IsOpaque)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    /// <summary>
    /// Runs on the server and, once it has accepted the click, on every client that can see the door — so each of
    /// them swings its own copy rather than being sent the result.
    /// </summary>
    public override void OnInteract(BlockState blockState, Vector3i blockPos, World world)
    {
        if (blockState is not BlockStateDoor door)
        {
            return;
        }

        bool open = !door.IsOpen;
        door.IsOpen = open;
        world.NotifyBlockStateChanged(blockPos, door);

        Vector3i otherHalf = OtherHalfOf(door, blockPos);
        if (world.GetBlockAt(otherHalf) is BlockStateDoor other && other.GetBlock() == this)
        {
            other.IsOpen = open;
            world.NotifyBlockStateChanged(otherHalf, other);
        }
    }

    private static Vector3i OtherHalfOf(BlockStateDoor door, Vector3i blockPos) =>
        door.IsUpper ? blockPos.Down() : blockPos.Up();
}
