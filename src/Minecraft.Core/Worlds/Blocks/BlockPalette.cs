namespace Minecraft.Core.Worlds.Blocks;

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
