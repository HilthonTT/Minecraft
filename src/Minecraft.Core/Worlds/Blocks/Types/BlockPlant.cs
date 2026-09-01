using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockPlant : Block
{
    private readonly Func<Block[]> _growsOnSelector;
    private Block[]? _growsOn;

    public BlockPlant(ushort id, Func<Block[]> growsOn) : base(id)
    {
        _growsOnSelector = growsOn;
        IsOpaque = false;
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0F;
    }

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
        const float Inset = 0.25F;

        var min = new Vector3(blockPos.X + Inset, blockPos.Y, blockPos.Z + Inset);
        var max = new Vector3(
            blockPos.X + Constants.CUBE_DIM - Inset,
            blockPos.Y + Constants.CUBE_DIM - Inset,
            blockPos.Z + Constants.CUBE_DIM - Inset);

        return [new AxisAlignedBox(min, max)];
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block below = world.GetBlockAt(blockPos.Down()).GetBlock();
        return Array.IndexOf(_growsOn ??= _growsOnSelector(), below) >= 0;
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

        if (blockPos == sourceBlockPos.Up() && world.GetBlockAt(sourceBlockPos).GetBlock() == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
