using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockLadder : Block
{
    public static readonly Direction[] WallSides = [Direction.Back, Direction.Right, Direction.Front, Direction.Left];

    public BlockLadder(ushort id) : base(id)
    {
        IsOpaque = false;
        HasCustomState = true;
        IsClimbable = true;
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 0.4F;
        HarvestTool = ToolKind.Axe;
    }

    public override BlockState GetNewDefaultState() => new BlockStateLadder(this);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos) =>
        BlockShapes.InWorld(BlockShapes.Ladder(AttachmentOf(state)), blockPos);

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos) =>
        GetCollisionBox(state, blockPos);

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        foreach (Direction side in WallSides)
        {
            if (IsWall(world, blockPos + DirectionUtil.ToUnit(side)))
            {
                return true;
            }
        }

        return false;
    }

    public override void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        // The client chose the wall against its own copy of the world. Settle on one the server can see before the
        // placement is passed on, so nobody is shown a ladder held up by nothing.
        if (world is WorldServer && blockState is BlockStateLadder ladder &&
            !IsWall(world, blockPos + DirectionUtil.ToUnit(ladder.Attachment)))
        {
            foreach (Direction side in WallSides)
            {
                if (IsWall(world, blockPos + DirectionUtil.ToUnit(side)))
                {
                    ladder.Attachment = side;
                    break;
                }
            }
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
        if (world is not WorldServer)
        {
            return;
        }

        Vector3i supportPos = blockPos + DirectionUtil.ToUnit(AttachmentOf(blockState));
        if (sourceBlockPos == supportPos && !IsWall(world, supportPos))
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    public static bool IsWall(World world, Vector3i blockPos) =>
        !world.IsOutsideBuildHeight(blockPos.Y) && world.GetBlockAt(blockPos).GetBlock().IsOpaque;

    private static Direction AttachmentOf(BlockState state) =>
        state is BlockStateLadder ladder ? ladder.Attachment : Direction.Front;
}
