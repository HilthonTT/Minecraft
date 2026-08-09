using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

/// <summary>
/// Every block the inventory screen offers, in the order it lays them out, and what each one is called.
/// <para>
/// Grouped by what a block is for rather than by the ids it was registered under, since the screen is read by
/// somebody looking for a building material and not by somebody looking up a number. Air is left out, having
/// nothing to show, and so is running water, which is only ever put down by a source spreading out from where
/// one was placed and is not a thing to be laid by hand.
/// </para>
/// </summary>
public static class BlockCatalogue
{
    /// <summary>How many blocks a row of the screen holds, which is also the width of the hotbar.</summary>
    public const int Columns = Inventory.HotbarSlots;

    private static readonly (Block Block, string Name)[] _entries =
    [
        // Worked stone and timber: what a house is made of.
        (BlockRegistry.Stone, "Stone"),
        (BlockRegistry.Cobblestone, "Cobblestone"),
        (BlockRegistry.MossyCobblestone, "Mossy Cobblestone"),
        (BlockRegistry.SandStone, "Sandstone"),
        (BlockRegistry.Planks, "Planks"),
        (BlockRegistry.OakLog, "Oak Log"),
        (BlockRegistry.BirchLog, "Birch Log"),
        (BlockRegistry.SpruceLog, "Spruce Log"),
        (BlockRegistry.Bedrock, "Bedrock"),

        // Ground, and the things lying about in it.
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

        // What is buried, and what burns.
        (BlockRegistry.CoalOre, "Coal Ore"),
        (BlockRegistry.IronOre, "Iron Ore"),
        (BlockRegistry.GoldOre, "Gold Ore"),
        (BlockRegistry.RedstoneOre, "Redstone Ore"),
        (BlockRegistry.DiamondOre, "Diamond Ore"),
        (BlockRegistry.Glowstone, "Glowstone"),
        (BlockRegistry.Torch, "Torch"),
        (BlockRegistry.Tnt, "TNT"),

        // Greenery, which mostly needs something to grow out of.
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

    /// <summary>How many rows the whole catalogue takes at <see cref="Columns"/> across.</summary>
    public static int Rows => (Count + Columns - 1) / Columns;

    public static Block BlockAt(int index) => _entries[index].Block;

    /// <summary>
    /// What to call the given block. Blocks the catalogue does not list are still reachable by picking one
    /// off the world with the middle mouse button, so this has to answer for those too.
    /// </summary>
    public static string NameOf(Block block) => _names.TryGetValue(block, out string? name) ? name : "Block";
}
