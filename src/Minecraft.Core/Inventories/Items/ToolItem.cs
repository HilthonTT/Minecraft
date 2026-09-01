using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

public sealed class ToolItem : SpriteItem
{
    public ToolItem(ushort id, ToolKind kind, ToolMaterial material, Vector2 iconCell)
        : base(id, material.DisplayName() + " " + kind, iconCell)
    {
        Kind = kind;
        Material = material;
    }

    public ToolKind Kind { get; }

    public ToolMaterial Material { get; }

    public override int MaxStackSize => 1;

    public override int MaxDurability => Material.Durability();

    public int AttackDamage => Kind switch
    {
        ToolKind.Sword => Material.SwordDamage(),
        ToolKind.Axe => Material.SwordDamage() - 1,
        ToolKind.Pickaxe => Material.SwordDamage() - 2,
        ToolKind.Shovel => Material.SwordDamage() - 3,
        _ => 1,
    };
}
