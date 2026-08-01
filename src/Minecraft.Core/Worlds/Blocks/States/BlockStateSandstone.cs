namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateSandstone : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.SandStone;
    }
}
