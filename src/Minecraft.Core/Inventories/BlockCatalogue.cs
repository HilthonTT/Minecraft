using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

public static class BlockCatalogue
{
    public const int Columns = Inventory.HotbarSlots;

    private static readonly (Block Block, string Name)[] _entries =
    [
        (BlockRegistry.Stone, "Stone"),
        (BlockRegistry.Cobblestone, "Cobblestone"),
        (BlockRegistry.MossyCobblestone, "Mossy Cobblestone"),
        (BlockRegistry.SandStone, "Sandstone"),
        (BlockRegistry.Planks, "Planks"),
        (BlockRegistry.CraftingTable, "Crafting Table"),
        (BlockRegistry.OakLog, "Oak Log"),
        (BlockRegistry.BirchLog, "Birch Log"),
        (BlockRegistry.SpruceLog, "Spruce Log"),
        (BlockRegistry.Bedrock, "Bedrock"),

        (BlockRegistry.Grass, "Grass"),
        (BlockRegistry.SnowyGrass, "Snowy Grass"),
        (BlockRegistry.Dirt, "Dirt"),
        (BlockRegistry.Sand, "Sand"),
        (BlockRegistry.Gravel, "Gravel"),
        (BlockRegistry.Clay, "Clay"),
        (BlockRegistry.Snow, "Snow"),
        (BlockRegistry.Ice, "Ice"),
        (BlockRegistry.Water, "Water"),
        (BlockRegistry.OakLeaves, "Leaves"),

        (BlockRegistry.CoalOre, "Coal Ore"),
        (BlockRegistry.IronOre, "Iron Ore"),
        (BlockRegistry.GoldOre, "Gold Ore"),
        (BlockRegistry.RedstoneOre, "Redstone Ore"),
        (BlockRegistry.DiamondOre, "Diamond Ore"),
        (BlockRegistry.Glowstone, "Glowstone"),
        (BlockRegistry.Torch, "Torch"),
        (BlockRegistry.Tnt, "TNT"),

        (BlockRegistry.GrassBlade, "Tall Grass"),
        (BlockRegistry.Flower, "Rose"),
        (BlockRegistry.Dandelion, "Dandelion"),
        (BlockRegistry.RedMushroom, "Red Mushroom"),
        (BlockRegistry.BrownMushroom, "Brown Mushroom"),
        (BlockRegistry.DeadBush, "Dead Bush"),
        (BlockRegistry.Cactus, "Cactus"),
        (BlockRegistry.SugarCane, "Sugar Cane"),
        (BlockRegistry.Wheat, "Wheat"),
    ];

    private static readonly Dictionary<Block, string> _names =
        _entries.ToDictionary(entry => entry.Block, entry => entry.Name);

    public static int Count => _entries.Length;

    public static int Rows => (Count + Columns - 1) / Columns;

    public static Block BlockAt(int index) => _entries[index].Block;

    public static string NameOf(Block block) => _names.TryGetValue(block, out string? name) ? name : "Block";
}
