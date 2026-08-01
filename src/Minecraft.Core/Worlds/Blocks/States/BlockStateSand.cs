namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateSand : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Sand;
    }
}
