using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// An item drawn as a flat piece of artwork rather than as a block: a stick, a lump of coal, an ingot.
/// <para>
/// Where a block item is drawn by building the block itself and turning it, this wears one cell of the item
/// sheet and is drawn as the shape that cell cuts out of it — a quad in a slot, and a slab with sides in the
/// hand and on the ground, so that a pickaxe lying in the grass has a thickness to it. See
/// <c>Shapes.ItemAtlas</c> for the sheet and <c>Render.ItemSpriteMesh</c> for the shape.
/// </para>
/// </summary>
public class SpriteItem : Item
{
    public SpriteItem(ushort id, string name, Vector2 iconCell) : base(id, name)
    {
        IconCell = iconCell;
    }

    /// <summary>Which cell of the item sheet this is drawn from, in cells from its top left corner.</summary>
    public Vector2 IconCell { get; }
}
