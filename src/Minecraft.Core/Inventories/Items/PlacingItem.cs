using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// An item drawn as a sprite that puts down a block other than one named after it: seeds plant wheat, and a door
/// hangs a door, whose block on its own would be only half of one.
/// </summary>
public sealed class PlacingItem(ushort id, string name, Vector2 iconCell, Func<Block> places)
    : SpriteItem(id, name, iconCell)
{
    public override Block? PlacedBlock => places();
}
