namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateGravel : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Gravel;
    }
}
