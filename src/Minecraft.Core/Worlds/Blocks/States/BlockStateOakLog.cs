namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateOakLog : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.OakLog;
    }
}
