using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

/// <summary>
/// Some number of one block, sitting in a slot. A value type, so a slot holds a stack rather than a reference
/// to one: two slots that happen to contain the same block are still two separate piles of it.
/// </summary>
public readonly struct ItemStack
{
    /// <summary>How many of one block a single slot will hold.</summary>
    public const int MaxCount = 64;

    /// <summary>An empty slot. The default value is deliberately this, so a fresh array of slots is empty.</summary>
    public static readonly ItemStack Empty = default;

    /// <summary>Null in an empty stack, which is the only state a count of zero is allowed in.</summary>
    public Block? Block { get; }

    public int Count { get; }

    public bool IsEmpty => Block is null || Count <= 0;

    public ItemStack(Block block, int count)
    {
        // A stack of nothing and a stack of zero would be two ways of writing the same thing, and code that
        // tested one of them would silently miss the other.
        if (count <= 0)
        {
            Block = null;
            Count = 0;
            return;
        }

        Block = block;
        Count = Math.Min(count, MaxCount);
    }

    /// <summary>The same block in a different quantity. A count of zero or less comes back as <see cref="Empty"/>.</summary>
    public ItemStack WithCount(int count) => Block is null ? Empty : new ItemStack(Block, count);

    /// <summary>
    /// Whether these two piles are of the same thing and so can be poured together. An empty stack stacks
    /// with nothing, including another empty one, since merging two of those has no meaning.
    /// </summary>
    public bool CanStackWith(ItemStack other) => !IsEmpty && !other.IsEmpty && Block == other.Block;

    /// <summary>How many more of this block the stack could take before it is full.</summary>
    public int RemainingSpace => IsEmpty ? MaxCount : MaxCount - Count;
}
