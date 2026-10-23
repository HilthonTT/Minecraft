using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// Fences and glass panes: a narrow shape that reaches out to join whatever is beside it. The connections are
/// worked out on every world that holds the block, client and server alike, from the same neighbour
/// notifications, so the two agree without the server having to send each change.
/// </summary>
public sealed class BlockConnecting : Block
{
    private BlockConnecting(ushort id, bool isPane) : base(id)
    {
        IsPane = isPane;
        IsOpaque = false;
        HasCustomState = true;
    }

    public static BlockConnecting Fence(ushort id) => new(id, isPane: false)
    {
        SoundMaterial = BlockSoundMaterial.Wood,
        SecondsToBreak = 2.0F,
        HarvestTool = ToolKind.Axe,
    };

    public static BlockConnecting Pane(ushort id) => new(id, isPane: true)
    {
        SecondsToBreak = 0.3F,
        HidesFacesAgainstItself = true,
    };

    public bool IsPane { get; }

    // A pane breaks the way the glass it is cut from does: into nothing.
    public override ItemStack GetDrop(BlockState blockState) => IsPane ? ItemStack.Empty : new ItemStack(this, 1);

    public override BlockState GetNewDefaultState() => new BlockStateConnecting(this);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        if (state is not BlockStateConnecting connecting)
        {
            return _emptyAABB;
        }

        return BlockShapes.InWorld(
            IsPane ? BlockShapes.Pane(connecting) : BlockShapes.FenceCollision(connecting),
            blockPos);
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        if (state is not BlockStateConnecting connecting)
        {
            return _emptyAABB;
        }

        return BlockShapes.InWorld(IsPane ? BlockShapes.Pane(connecting) : BlockShapes.Fence(connecting), blockPos);
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        if (blockState is BlockStateConnecting connecting)
        {
            connecting.SetConnections(side => ConnectsTo(world, blockPos + DirectionUtil.ToUnit(side)));
        }

        base.OnAdd(blockState, world, blockPos);
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (blockState is not BlockStateConnecting connecting || sourceBlockPos.Y != blockPos.Y)
        {
            return;
        }

        if (connecting.SetConnections(side => ConnectsTo(world, blockPos + DirectionUtil.ToUnit(side))))
        {
            world.NotifyBlockStateChanged(blockPos, blockState);
        }
    }

    private bool ConnectsTo(World world, Vector3i neighbourPos)
    {
        Block neighbour = world.GetBlockAt(neighbourPos).GetBlock();

        if (neighbour == this || neighbour.IsOpaque)
        {
            return true;
        }

        return IsPane && neighbour == BlockRegistry.Glass;
    }
}
