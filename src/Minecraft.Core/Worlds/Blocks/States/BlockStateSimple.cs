namespace Minecraft.Core.Worlds.Blocks.States;

/// <summary>
/// The state of a block that carries no data of its own, holding nothing but which block it is.
/// <para>
/// Blocks whose state does mean something get a class per block, because there the class is where the data
/// lives. The stone, ores and plants the terrain is built out of have nothing to remember, and a class each
/// for them would be the same three lines repeated a dozen times over.
/// </para>
/// </summary>
public sealed class BlockStateSimple(Block block) : BlockState
{
    public override Block GetBlock() => block;

    public override string ToString() => block.GetType().Name + "[" + block.Id + "]";
}
