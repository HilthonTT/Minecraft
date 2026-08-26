using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// A block, held rather than standing in the world. One is made for every block in the registry, and carries
/// that block's own id, so a stack written down before there were items at all still reads back as the same
/// thing.
/// <para>
/// This is the only kind of item a right click can put down. Everything else in a hand is something to build
/// <em>with</em> rather than something to build out of, and reaching out with one does nothing.
/// </para>
/// </summary>
public sealed class BlockItem : Item
{
    public BlockItem(Block block, string name) : base(block.Id, name)
    {
        Block = block;
    }

    public Block Block { get; }
}
