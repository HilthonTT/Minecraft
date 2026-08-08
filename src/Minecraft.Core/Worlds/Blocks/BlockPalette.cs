namespace Minecraft.Core.Worlds.Blocks;

/// <summary>
/// The nine blocks a player starts a world carrying, in the order they are laid along the hotbar.
/// <para>
/// Not a limit on what can be held — anything in the game can be dragged into a slot from the inventory
/// screen, or picked off the world with the middle mouse button. This is only what is already there on the
/// way in, so that a new world can be built in without opening a screen first.
/// </para>
/// </summary>
public static class BlockPalette
{
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
