namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateStone : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Stone;
    }
}
