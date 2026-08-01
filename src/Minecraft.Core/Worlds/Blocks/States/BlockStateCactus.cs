namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateCactus : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Cactus;
    }
}
