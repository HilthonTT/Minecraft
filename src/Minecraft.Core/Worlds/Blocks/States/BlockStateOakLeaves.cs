namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateOakLeaves : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.OakLeaves;
    }
}
