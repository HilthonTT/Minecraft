namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateCobblestone : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Cobblestone;
    }
}
