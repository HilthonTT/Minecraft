using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

/// <summary>
/// Some number of one item, sitting in a slot. A value type, so a slot holds a stack rather than a reference
/// to one: two slots that happen to contain the same item are still two separate piles of it.
/// <para>
/// A tool is a stack of one carrying wear, which is why <see cref="Damage"/> lives here rather than on the
/// item: two pickaxes are the same item and are not the same pickaxe, and only the thing in the slot can know
/// which of them has been swung more.
/// </para>
/// </summary>
public readonly struct ItemStack
{
    /// <summary>The most any slot will hold, whatever is in it. An item may cap itself lower.</summary>
    public const int MaxCount = 64;

    /// <summary>An empty slot. The default value is deliberately this, so a fresh array of slots is empty.</summary>
    public static readonly ItemStack Empty = default;

    /// <summary>Null in an empty stack, which is the only state a count of zero is allowed in.</summary>
    public Item? Item { get; }

    public int Count { get; }

    /// <summary>
    /// How much use has been taken out of this, from none of it up to the item's own durability. Always zero
    /// for anything that does not wear out.
    /// </summary>
    public int Damage { get; }

    public bool IsEmpty => Item is null || Count <= 0;

    public ItemStack(Item item, int count, int damage = 0)
    {
        // A stack of nothing and a stack of zero would be two ways of writing the same thing, and code that
        // tested one of them would silently miss the other.
        if (count <= 0)
        {
            Item = null;
            Count = 0;
            Damage = 0;
            return;
        }

        Item = item;
        Count = Math.Min(count, item.MaxStackSize);
        Damage = item.IsDamageable ? Math.Clamp(damage, 0, item.MaxDurability) : 0;
    }

    /// <summary>A pile of a block, which is the shape most of what the world hands out arrives in.</summary>
    public ItemStack(Block block, int count) : this(ItemRegistry.For(block), count)
    {
    }

    /// <summary>The block this puts down, or null for something that is not a block and so cannot be placed.</summary>
    public Block? Block => (Item as BlockItem)?.Block;

    /// <summary>This as a tool, or null when it is not one.</summary>
    public ToolItem? Tool => Item as ToolItem;

    /// <summary>How many of this one slot will hold, which a tool caps at one.</summary>
    public int MaxStackSize => Item?.MaxStackSize ?? MaxCount;

    /// <summary>How much use is left before this is gone. Zero for anything that does not wear out.</summary>
    public int RemainingDurability => Item is null || !Item.IsDamageable ? 0 : Item.MaxDurability - Damage;

    /// <summary>The same item in a different quantity. A count of zero or less comes back as <see cref="Empty"/>.</summary>
    public ItemStack WithCount(int count) => Item is null ? Empty : new ItemStack(Item, count, Damage);

    /// <summary>
    /// The same stack after being used once more. Comes back empty when that was the last of it, which is
    /// how a tool breaking is reported: the slot holding it simply stops holding anything.
    /// </summary>
    public ItemStack Worn(int by = 1)
    {
        if (Item is null || !Item.IsDamageable || by <= 0)
        {
            return this;
        }

        int damage = Damage + by;
        return damage >= Item.MaxDurability ? Empty : new ItemStack(Item, Count, damage);
    }

    /// <summary>
    /// Whether these two piles are of the same thing and so can be poured together. An empty stack stacks
    /// with nothing, including another empty one, since merging two of those has no meaning, and neither does
    /// anything that only ever comes one to a slot: two worn pickaxes are not one pickaxe worn twice.
    /// </summary>
    public bool CanStackWith(ItemStack other) =>
        Item is not null && Count > 0 && !other.IsEmpty && Item == other.Item && Item.MaxStackSize > 1;

    /// <summary>How many more of this item the stack could take before it is full.</summary>
    public int RemainingSpace => IsEmpty ? MaxCount : MaxStackSize - Count;
}
