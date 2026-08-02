namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStatePlanks : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Planks;
    }
}
