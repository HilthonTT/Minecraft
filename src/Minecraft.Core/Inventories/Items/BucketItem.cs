using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

public sealed class BucketItem(ushort id, string name, Vector2 iconCell, Func<Block?> holds)
    : SpriteItem(id, name, iconCell)
{
    private const int EmptyStackSize = 16;

    /// <summary>The fluid source a full bucket pours out, or null for an empty one.</summary>
    public Block? Holds => holds();

    public bool IsEmpty => Holds is null;

    public override int MaxStackSize => IsEmpty ? EmptyStackSize : 1;
}
