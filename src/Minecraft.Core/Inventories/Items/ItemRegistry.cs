using Minecraft.Core.Shapes;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// The single source of truth for everything a slot can hold, and where the ids that travel over the wire are
/// assigned.
/// <para>
/// Two halves share one run of ids. Every block gets an item carrying the block's own id, filled in by walking
/// <see cref="BlockRegistry"/> rather than by naming each of them again here, so a block added there needs
/// nothing done to it to become something that can be carried. Everything that is not a block starts at
/// <see cref="FirstLooseItemId"/>, well clear of the block ids, so that the two can grow without either
/// reaching the other.
/// </para>
/// </summary>
public static class ItemRegistry
{
    /// <summary>
    /// Where the ids of things that are not blocks begin. Far enough above the blocks that a good many more
    /// of them could be registered before the two ran together, and a round number so that an id read out of
    /// a log says at a glance which half it came from.
    /// </summary>
    public const ushort FirstLooseItemId = 256;

    public static readonly SpriteItem Stick = new(256, "Stick", ItemAtlas.Stick);
    public static readonly SpriteItem Coal = new(257, "Coal", ItemAtlas.Coal);
    public static readonly SpriteItem IronIngot = new(258, "Iron Ingot", ItemAtlas.IronIngot);
    public static readonly SpriteItem GoldIngot = new(259, "Gold Ingot", ItemAtlas.GoldIngot);
    public static readonly SpriteItem Diamond = new(260, "Diamond", ItemAtlas.Diamond);
    public static readonly SpriteItem Redstone = new(261, "Redstone", ItemAtlas.Redstone);

    public static readonly ToolItem WoodenPickaxe = new(262, ToolKind.Pickaxe, ToolMaterial.Wood, ItemAtlas.Pickaxe(0));
    public static readonly ToolItem StonePickaxe = new(263, ToolKind.Pickaxe, ToolMaterial.Stone, ItemAtlas.Pickaxe(1));
    public static readonly ToolItem IronPickaxe = new(264, ToolKind.Pickaxe, ToolMaterial.Iron, ItemAtlas.Pickaxe(2));
    public static readonly ToolItem GoldenPickaxe = new(265, ToolKind.Pickaxe, ToolMaterial.Gold, ItemAtlas.Pickaxe(3));
    public static readonly ToolItem DiamondPickaxe = new(266, ToolKind.Pickaxe, ToolMaterial.Diamond, ItemAtlas.Pickaxe(4));

    public static readonly ToolItem WoodenAxe = new(267, ToolKind.Axe, ToolMaterial.Wood, ItemAtlas.Axe(0));
    public static readonly ToolItem StoneAxe = new(268, ToolKind.Axe, ToolMaterial.Stone, ItemAtlas.Axe(1));
    public static readonly ToolItem IronAxe = new(269, ToolKind.Axe, ToolMaterial.Iron, ItemAtlas.Axe(2));
    public static readonly ToolItem GoldenAxe = new(270, ToolKind.Axe, ToolMaterial.Gold, ItemAtlas.Axe(3));
    public static readonly ToolItem DiamondAxe = new(271, ToolKind.Axe, ToolMaterial.Diamond, ItemAtlas.Axe(4));

    public static readonly ToolItem WoodenShovel = new(272, ToolKind.Shovel, ToolMaterial.Wood, ItemAtlas.Shovel(0));
    public static readonly ToolItem StoneShovel = new(273, ToolKind.Shovel, ToolMaterial.Stone, ItemAtlas.Shovel(1));
    public static readonly ToolItem IronShovel = new(274, ToolKind.Shovel, ToolMaterial.Iron, ItemAtlas.Shovel(2));
    public static readonly ToolItem GoldenShovel = new(275, ToolKind.Shovel, ToolMaterial.Gold, ItemAtlas.Shovel(3));
    public static readonly ToolItem DiamondShovel = new(276, ToolKind.Shovel, ToolMaterial.Diamond, ItemAtlas.Shovel(4));

    public static readonly ToolItem WoodenSword = new(277, ToolKind.Sword, ToolMaterial.Wood, ItemAtlas.Sword(0));
    public static readonly ToolItem StoneSword = new(278, ToolKind.Sword, ToolMaterial.Stone, ItemAtlas.Sword(1));
    public static readonly ToolItem IronSword = new(279, ToolKind.Sword, ToolMaterial.Iron, ItemAtlas.Sword(2));
    public static readonly ToolItem GoldenSword = new(280, ToolKind.Sword, ToolMaterial.Gold, ItemAtlas.Sword(3));
    public static readonly ToolItem DiamondSword = new(281, ToolKind.Sword, ToolMaterial.Diamond, ItemAtlas.Sword(4));

    /// <summary>Every registered item, indexed by its own id. Sparse: the gap under the loose ids is empty.</summary>
    private static Item?[] _byId = [];

    /// <summary>The item for each block, indexed by block id, which is also that item's id.</summary>
    private static BlockItem[] _byBlockId = [];

    /// <summary>Everything that is not a block, in the order the creative screen lays them out.</summary>
    public static IReadOnlyList<SpriteItem> LooseItems => _looseItems;

    private static readonly SpriteItem[] _looseItems =
    [
        Stick, Coal, IronIngot, GoldIngot, Diamond, Redstone,
        WoodenPickaxe, StonePickaxe, IronPickaxe, GoldenPickaxe, DiamondPickaxe,
        WoodenAxe, StoneAxe, IronAxe, GoldenAxe, DiamondAxe,
        WoodenShovel, StoneShovel, IronShovel, GoldenShovel, DiamondShovel,
        WoodenSword, StoneSword, IronSword, GoldenSword, DiamondSword,
    ];

    /// <summary>
    /// Builds the table. Must be called after <see cref="BlockRegistry.RegisterBlocks"/>, since half of what
    /// goes in it is made out of what that registered.
    /// </summary>
    public static void RegisterItems()
    {
        // The two halves share one run of numbers, so the blocks have to stop short of where the loose items
        // begin. A block registered past that point would take an id an item already answers to, and quietly
        // change what a number already written into a save and sent over the wire means.
        if (BlockRegistry.Count >= FirstLooseItemId)
        {
            throw new InvalidOperationException(
                $"There are {BlockRegistry.Count} blocks, which reaches into the range reserved for items at "
                + $"{FirstLooseItemId}. Raise FirstLooseItemId and renumber the items above it.");
        }

        _byBlockId = new BlockItem[BlockRegistry.Count + 1];

        ushort highestId = FirstLooseItemId;
        foreach (SpriteItem item in _looseItems)
        {
            highestId = Math.Max(highestId, item.Id);
        }

        _byId = new Item?[highestId + 1];

        for (int id = 1; id <= BlockRegistry.Count; id++)
        {
            Block block = BlockRegistry.GetBlockFromIdentifier(id);
            var item = new BlockItem(block, BlockCatalogue.NameOf(block));

            _byBlockId[id] = item;
            _byId[id] = item;
        }

        foreach (SpriteItem item in _looseItems)
        {
            if (item.Id < FirstLooseItemId)
            {
                throw new InvalidOperationException(
                    $"Item {item.Name} has id {item.Id}, which is inside the range reserved for blocks.");
            }

            if (_byId[item.Id] is not null)
            {
                throw new InvalidOperationException($"Item id {item.Id} is registered twice.");
            }

            _byId[item.Id] = item;
        }
    }

    /// <summary>The item that puts the given block down. Every block has one.</summary>
    public static BlockItem For(Block block)
    {
        if (block.Id >= _byBlockId.Length)
        {
            throw new InvalidOperationException(
                $"Asked for the item of block {block.Id} before RegisterItems ran. Blocks are registered first, "
                + "and every item is made out of what that step registered.");
        }

        return _byBlockId[block.Id];
    }

    /// <summary>
    /// The item with the given id, or null when there is none. For ids that came off the wire, where an
    /// unknown one is something to turn down rather than something to fall over.
    /// </summary>
    public static Item? TryGet(int id) =>
        id >= 0 && id < _byId.Length ? _byId[id] : null;
}
