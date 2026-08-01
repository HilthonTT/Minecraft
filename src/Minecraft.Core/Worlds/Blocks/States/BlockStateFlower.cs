namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateFlower : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Flower;
    }
}
