namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateDirt : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Dirt;
    }
}
