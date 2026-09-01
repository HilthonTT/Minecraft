namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateSimple(Block block) : BlockState
{
    public override Block GetBlock() => block;

    public override string ToString() => block.GetType().Name + "[" + block.Id + "]";
}
