namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateAir : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.Air;
    }
}
