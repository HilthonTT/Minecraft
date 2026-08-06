namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateWater : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Water;
    }
}
