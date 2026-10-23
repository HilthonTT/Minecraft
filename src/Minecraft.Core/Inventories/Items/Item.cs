using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

public abstract class Item
{
    protected Item(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    public ushort Id { get; }

    public string Name { get; }

    public virtual int MaxStackSize => ItemStack.MaxCount;

    public virtual int MaxDurability => 0;

    public bool IsDamageable => MaxDurability > 0;

    /// <summary>The block a right click with this in hand puts down, if it puts one down at all.</summary>
    public virtual Block? PlacedBlock => null;

    public override string ToString() => Name;
}
