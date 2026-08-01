namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateDeadBush : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.DeadBush;
    }
}
