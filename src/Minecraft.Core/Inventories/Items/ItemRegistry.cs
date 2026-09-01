using Minecraft.Core.Shapes;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

public static class ItemRegistry
{
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

    private static Item?[] _byId = [];

    private static BlockItem[] _byBlockId = [];

    public static IReadOnlyList<SpriteItem> LooseItems => _looseItems;

    private static readonly SpriteItem[] _looseItems =
    [
        Stick, Coal, IronIngot, GoldIngot, Diamond, Redstone,
        WoodenPickaxe, StonePickaxe, IronPickaxe, GoldenPickaxe, DiamondPickaxe,
        WoodenAxe, StoneAxe, IronAxe, GoldenAxe, DiamondAxe,
        WoodenShovel, StoneShovel, IronShovel, GoldenShovel, DiamondShovel,
        WoodenSword, StoneSword, IronSword, GoldenSword, DiamondSword,
    ];

    public static void RegisterItems()
    {
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

    public static Item? TryGet(int id) =>
        id >= 0 && id < _byId.Length ? _byId[id] : null;
}
