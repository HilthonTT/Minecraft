using Minecraft.Core.Physics;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockStairs : Block
{
    public BlockStairs(ushort id, Block fullBlock) : base(id)
    {
        IsOpaque = false;
        HasCustomState = true;

        SoundMaterial = fullBlock.SoundMaterial;
        SecondsToBreak = fullBlock.SecondsToBreak;
        HarvestTool = fullBlock.HarvestTool;
        RequiresCorrectTool = fullBlock.RequiresCorrectTool;
        HarvestLevel = fullBlock.HarvestLevel;
    }

    public override BlockState GetNewDefaultState() => new BlockStateStairs(this);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        var stairs = state as BlockStateStairs;
        return BlockShapes.InWorld(
            BlockShapes.Stairs(stairs?.Facing ?? Direction.Front, stairs?.IsUpsideDown ?? false),
            blockPos);
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos) =>
        GetCollisionBox(state, blockPos);
}
