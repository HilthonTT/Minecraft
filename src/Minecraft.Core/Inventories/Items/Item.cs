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

    public override string ToString() => Name;
}
