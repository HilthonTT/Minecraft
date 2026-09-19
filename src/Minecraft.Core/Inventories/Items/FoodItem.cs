using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

public sealed class FoodItem : SpriteItem
{
    public FoodItem(ushort id, string name, Vector2 iconCell, int nourishment) : base(id, name, iconCell)
    {
        Nourishment = nourishment;
    }

    public int Nourishment { get; }
}
