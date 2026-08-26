namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// What a tool is made of, which is the whole of the difference between two tools of the same kind: how fast
/// it digs, how much is buried too deep for it to earn anything from, how long it lasts, and how hard it hits.
/// <para>
/// The five are a ladder, and the rungs are not evenly spaced. Gold is off to one side of it rather than on
/// it: it digs faster than diamond and wears out faster than wood, so it is a thing to spend a lucky vein on
/// rather than a step on the way anywhere.
/// </para>
/// </summary>
public enum ToolMaterial
{
    Wood,
    Stone,
    Iron,
    Gold,
    Diamond,
}
