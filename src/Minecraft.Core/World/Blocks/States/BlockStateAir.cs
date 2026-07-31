namespace Minecraft.Core.World.Blocks.States;

public sealed class BlockStateAir : BlockState
{
    public override Block GetBlock()
    {
        return Blocks.Air;
    }
}
