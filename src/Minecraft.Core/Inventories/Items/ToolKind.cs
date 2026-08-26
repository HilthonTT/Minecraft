namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// What a tool is for. A block names the one kind that digs it faster, and a tool of any other kind is worth
/// no more against it than a bare hand.
/// <para>
/// A sword is in here with the digging tools because it is held and worn out the same way, not because there
/// is anything it is the right tool for: no block names it, so it never speeds a break up and never earns a
/// drop. What it is for is what it does to a mob.
/// </para>
/// </summary>
public enum ToolKind
{
    Pickaxe,
    Axe,
    Shovel,
    Sword,
}
