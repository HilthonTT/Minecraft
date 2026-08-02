using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// The blocks a settlement in one biome is built out of. A biome without a palette is never settled, which is
/// how mountains stay empty.
/// </summary>
/// <param name="Foundation">Fills the gap between a levelled floor and the ground under it.</param>
/// <param name="Floor">The floor a building stands on.</param>
/// <param name="Wall">The bulk of a building's walls.</param>
/// <param name="Corner">The posts at a building's corners, and its door frame.</param>
/// <param name="Roof">Both roof layers.</param>
/// <param name="Path">The roads between buildings, laid over the surface block.</param>
/// <param name="Fence">The low wall around a farm.</param>
/// <param name="FarmSoil">
/// What crops are planted in. Wheat uproots itself when what is under it changes, so this has to stay a
/// block wheat accepts.
/// </param>
public readonly record struct StructurePalette(
    Block Foundation,
    Block Floor,
    Block Wall,
    Block Corner,
    Block Roof,
    Block Path,
    Block Fence,
    Block FarmSoil)
{
    /// <summary>Timber and cobblestone, for the grassy biomes.</summary>
    public static StructurePalette Oak { get; } = new(
        Foundation: BlockRegistry.Cobblestone,
        Floor: BlockRegistry.Planks,
        Wall: BlockRegistry.Planks,
        Corner: BlockRegistry.OakLog,
        Roof: BlockRegistry.OakLog,
        Path: BlockRegistry.Gravel,
        Fence: BlockRegistry.OakLog,
        FarmSoil: BlockRegistry.Grass);

    /// <summary>Sandstone throughout, for the desert.</summary>
    public static StructurePalette Sandstone { get; } = new(
        Foundation: BlockRegistry.SandStone,
        Floor: BlockRegistry.SandStone,
        Wall: BlockRegistry.SandStone,
        Corner: BlockRegistry.Cobblestone,
        Roof: BlockRegistry.SandStone,
        Path: BlockRegistry.Gravel,
        Fence: BlockRegistry.Cobblestone,
        FarmSoil: BlockRegistry.Grass);
}
