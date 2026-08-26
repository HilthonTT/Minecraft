namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// One kind of thing a slot can hold. The layer the inventory, the hotbar and the ground drops are written
/// against, so that a pickaxe and a stack of dirt travel the same roads.
/// <para>
/// Ids are assigned in <see cref="ItemRegistry"/> and must stay stable, since they are what travels over the
/// wire. Every block has an item of its own carrying the block's own id, so the ids a stack has always been
/// written down as still mean what they did; anything that is not a block starts above the block ids and
/// leaves room for the world to grow into the gap. See <see cref="ItemRegistry.FirstLooseItemId"/>.
/// </para>
/// </summary>
public abstract class Item
{
    protected Item(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    public ushort Id { get; }

    /// <summary>What the interface calls this, under the cursor and over the hotbar.</summary>
    public string Name { get; }

    /// <summary>
    /// How many of these one slot will hold. Sixty four for anything there can be a pile of, and one for a
    /// tool, which carries wear of its own and so cannot be poured together with another.
    /// </summary>
    public virtual int MaxStackSize => ItemStack.MaxCount;

    /// <summary>
    /// How much use this has in it before it is gone, or zero for something that never wears out. Only a
    /// stack of one can carry wear, which is why this and <see cref="MaxStackSize"/> move together.
    /// </summary>
    public virtual int MaxDurability => 0;

    public bool IsDamageable => MaxDurability > 0;

    public override string ToString() => Name;
}
