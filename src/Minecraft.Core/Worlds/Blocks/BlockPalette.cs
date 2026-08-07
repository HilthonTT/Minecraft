namespace Minecraft.Core.Worlds.Blocks;

/// <summary>
/// What the player can reach for directly, by number key or by the mouse wheel.
/// <para>
/// A fixed list rather than an inventory: nothing is collected, counted or spent, so this is only a way of
/// naming a block to build with. Anything else in the world is still reachable by picking it with the middle
/// mouse button. Kept to nine so the whole of it sits under the number row.
/// </para>
/// </summary>
public static class BlockPalette
{
    /// <summary>Where the given block sits in the palette, or -1 for one that is not in it at all.</summary>
    public static int IndexOf(Block block) => Array.IndexOf(_blocks, block);

    public static IReadOnlyList<Block> Blocks => _blocks;

    private static readonly Block[] _blocks =
    [
        BlockRegistry.Torch,
        BlockRegistry.Planks,
        BlockRegistry.Cobblestone,
        BlockRegistry.Stone,
        BlockRegistry.Dirt,
        BlockRegistry.Sand,
        BlockRegistry.OakLog,
        BlockRegistry.Glowstone,
        BlockRegistry.Tnt,
    ];
}
