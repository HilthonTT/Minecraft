using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

public readonly struct ItemStack
{
    public const int MaxCount = 64;

    public static readonly ItemStack Empty = default;

    public Item? Item { get; }

    public int Count { get; }

    public int Damage { get; }

    public bool IsEmpty => Item is null || Count <= 0;

    public ItemStack(Item item, int count, int damage = 0)
    {
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

    public ItemStack(Block block, int count) : this(ItemRegistry.For(block), count)
    {
    }

    public Block? Block => (Item as BlockItem)?.Block;

    public ToolItem? Tool => Item as ToolItem;

    public int MaxStackSize => Item?.MaxStackSize ?? MaxCount;

    public int RemainingDurability => Item is null || !Item.IsDamageable ? 0 : Item.MaxDurability - Damage;

    public ItemStack WithCount(int count) => Item is null ? Empty : new ItemStack(Item, count, Damage);

    public ItemStack Worn(int by = 1)
    {
        if (Item is null || !Item.IsDamageable || by <= 0)
        {
            return this;
        }

        int damage = Damage + by;
        return damage >= Item.MaxDurability ? Empty : new ItemStack(Item, Count, damage);
    }

    public bool CanStackWith(ItemStack other) =>
        Item is not null && Count > 0 && !other.IsEmpty && Item == other.Item && Item.MaxStackSize > 1;

    public int RemainingSpace => IsEmpty ? MaxCount : MaxStackSize - Count;
}
