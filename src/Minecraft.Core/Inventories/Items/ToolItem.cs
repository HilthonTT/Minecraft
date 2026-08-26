using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// Something held in order to dig faster, reach deeper or hit harder, and which wears out doing it.
/// <para>
/// A tool is a kind and a material and nothing else. The kind decides which blocks it is the right thing to
/// swing at, and the material decides everything about how well it does: see <see cref="ToolMaterial"/>,
/// where the whole of the ladder is written down in one place.
/// </para>
/// </summary>
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

    /// <summary>A tool carries wear, and a slot holding wear can only hold the one thing.</summary>
    public override int MaxStackSize => 1;

    public override int MaxDurability => Material.Durability();

    /// <summary>
    /// Half hearts taken off a mob by a blow from this. A sword is what the numbers in
    /// <see cref="ToolMaterials.SwordDamage"/> are for; the digging tools are worth one less each, in the
    /// order they are worse shaped for it, which puts a wooden shovel level with the bare fist it replaced.
    /// </summary>
    public int AttackDamage => Kind switch
    {
        ToolKind.Sword => Material.SwordDamage(),
        ToolKind.Axe => Material.SwordDamage() - 1,
        ToolKind.Pickaxe => Material.SwordDamage() - 2,
        ToolKind.Shovel => Material.SwordDamage() - 3,
        _ => 1,
    };
}
