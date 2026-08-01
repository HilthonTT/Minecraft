namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateGrassBlade : BlockState
{
    public override Block GetBlock()
    {
        return BlockRegistry.GrassBlade;
    }
}
