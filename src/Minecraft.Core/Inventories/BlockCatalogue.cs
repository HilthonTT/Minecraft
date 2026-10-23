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
        (BlockRegistry.Furnace, "Furnace"),
        (BlockRegistry.Chest, "Chest"),
        (BlockRegistry.OakLog, "Oak Log"),
        (BlockRegistry.BirchLog, "Birch Log"),
        (BlockRegistry.SpruceLog, "Spruce Log"),
        (BlockRegistry.Bedrock, "Bedrock"),
        (BlockRegistry.Obsidian, "Obsidian"),
        (BlockRegistry.Glass, "Glass"),
        (BlockRegistry.GlassPane, "Glass Pane"),
        (BlockRegistry.Wool, "Wool"),

        (BlockRegistry.StoneSlab, "Stone Slab"),
        (BlockRegistry.CobblestoneSlab, "Cobblestone Slab"),
        (BlockRegistry.PlanksSlab, "Wooden Slab"),
        (BlockRegistry.SandstoneSlab, "Sandstone Slab"),
        (BlockRegistry.PlanksStairs, "Wooden Stairs"),
        (BlockRegistry.CobblestoneStairs, "Cobblestone Stairs"),
        (BlockRegistry.Fence, "Fence"),
        (BlockRegistry.Ladder, "Ladder"),

        (BlockRegistry.Grass, "Grass"),
        (BlockRegistry.SnowyGrass, "Snowy Grass"),
        (BlockRegistry.Dirt, "Dirt"),
        (BlockRegistry.Farmland, "Farmland"),
        (BlockRegistry.Sand, "Sand"),
        (BlockRegistry.Gravel, "Gravel"),
        (BlockRegistry.Clay, "Clay"),
        (BlockRegistry.Snow, "Snow"),
        (BlockRegistry.Ice, "Ice"),
        (BlockRegistry.Water, "Water"),
        (BlockRegistry.Lava, "Lava"),
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
        (BlockRegistry.OakSapling, "Oak Sapling"),
        (BlockRegistry.BirchSapling, "Birch Sapling"),
        (BlockRegistry.SpruceSapling, "Spruce Sapling"),
    ];

    // Blocks with a name but no place in the list, because something else puts them down: the door item hangs a
    // door, and a door block on its own would be only half of one.
    private static readonly (Block Block, string Name)[] _unlisted =
    [
        (BlockRegistry.Door, "Door"),
    ];

    private static readonly Dictionary<Block, string> _names =
        _entries.Concat(_unlisted).ToDictionary(entry => entry.Block, entry => entry.Name);

    public static int Count => _entries.Length;

    public static int Rows => (Count + Columns - 1) / Columns;

    public static Block BlockAt(int index) => _entries[index].Block;

    public static string NameOf(Block block) => _names.TryGetValue(block, out string? name) ? name : "Block";
}
