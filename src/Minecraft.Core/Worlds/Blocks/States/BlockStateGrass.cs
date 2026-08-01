namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateGrass : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Grass;
    }
}
