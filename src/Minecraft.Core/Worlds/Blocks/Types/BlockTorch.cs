using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockTorch : Block
{
    private const float SelectionInset = 0.36F;

    private const float StickHeight = 0.65F;

    public BlockTorch(ushort id) : base(id)
    {
        IsOpaque = false;
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 0F;

        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateTorch();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        var min = new Vector3(blockPos.X + SelectionInset, blockPos.Y, blockPos.Z + SelectionInset);
        var max = new Vector3(
            blockPos.X + Constants.CUBE_DIM - SelectionInset,
            blockPos.Y + StickHeight,
            blockPos.Z + Constants.CUBE_DIM - SelectionInset);

        if (state is BlockStateTorch torch && torch.IsOnWall)
        {
            Vector3i towardsWall = DirectionUtil.ToUnit(torch.Attachment);
            var shift = new Vector3(towardsWall.X * 0.3F, 0.2F, towardsWall.Z * 0.3F);
            min += shift;
            max += shift;
        }

        return [new AxisAlignedBox(min, max)];
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        if (IsSupporting(world, blockPos.Down()))
        {
            return true;
        }

        foreach (Direction side in _wallSides)
        {
            if (IsSupporting(world, blockPos + DirectionUtil.ToUnit(side)))
            {
                return true;
            }
        }

        return false;
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer || blockState is not BlockStateTorch torch)
        {
            return;
        }

        Vector3i supportPos = blockPos + DirectionUtil.ToUnit(torch.Attachment);
        if (sourceBlockPos == supportPos && !IsSupporting(world, supportPos))
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    private static readonly Direction[] _wallSides =
    [
        Direction.Back,
        Direction.Right,
        Direction.Front,
        Direction.Left,
    ];

    private static bool IsSupporting(World world, Vector3i blockPos)
    {
        return !world.IsOutsideBuildHeight(blockPos.Y) && world.GetBlockAt(blockPos).GetBlock().IsOpaque;
    }
}
