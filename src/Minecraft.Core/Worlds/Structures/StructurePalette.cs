using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures;

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
    public static StructurePalette Oak { get; } = new(
        Foundation: BlockRegistry.Cobblestone,
        Floor: BlockRegistry.Planks,
        Wall: BlockRegistry.Planks,
        Corner: BlockRegistry.OakLog,
        Roof: BlockRegistry.OakLog,
        Path: BlockRegistry.Gravel,
        Fence: BlockRegistry.OakLog,
        FarmSoil: BlockRegistry.Grass);

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
