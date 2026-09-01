using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

public class SpriteItem : Item
{
    public SpriteItem(ushort id, string name, Vector2 iconCell) : base(id, name)
    {
        IconCell = iconCell;
    }

    public Vector2 IconCell { get; }
}
