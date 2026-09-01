using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

public sealed class BlockItem : Item
{
    public BlockItem(Block block, string name) : base(block.Id, name)
    {
        Block = block;
    }

    public Block Block { get; }
}
