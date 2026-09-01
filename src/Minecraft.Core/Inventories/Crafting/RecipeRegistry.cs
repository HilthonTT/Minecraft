using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Crafting;

public static class RecipeRegistry
{
    private static readonly (ToolMaterial Material, Func<Item> Bar)[] _toolMaterials =
    [
        (ToolMaterial.Wood, () => ItemRegistry.For(BlockRegistry.Planks)),
        (ToolMaterial.Stone, () => ItemRegistry.For(BlockRegistry.Cobblestone)),
        (ToolMaterial.Iron, () => ItemRegistry.IronIngot),
        (ToolMaterial.Gold, () => ItemRegistry.GoldIngot),
        (ToolMaterial.Diamond, () => ItemRegistry.Diamond),
    ];

    private static readonly (ToolKind Kind, string[] Pattern)[] _toolShapes =
    [
        (ToolKind.Pickaxe, ["XXX", " S ", " S "]),
        (ToolKind.Axe, ["XX", "XS", " S"]),
        (ToolKind.Shovel, ["X", "S", "S"]),
        (ToolKind.Sword, ["X", "X", "S"]),
    ];

    private static Recipe[] _recipes = [];

    public static void RegisterRecipes()
    {
        Item planks = ItemRegistry.For(BlockRegistry.Planks);
        Item stick = ItemRegistry.Stick;

        List<Recipe> recipes =
        [
            Recipe.Shapeless(new ItemStack(planks, 4), ItemRegistry.For(BlockRegistry.OakLog)),
            Recipe.Shapeless(new ItemStack(planks, 4), ItemRegistry.For(BlockRegistry.BirchLog)),
            Recipe.Shapeless(new ItemStack(planks, 4), ItemRegistry.For(BlockRegistry.SpruceLog)),

            Recipe.Shaped(new ItemStack(stick, 4), ["X", "X"], new Dictionary<char, Item> { ['X'] = planks }),

            Recipe.Shaped(
                new ItemStack(ItemRegistry.For(BlockRegistry.CraftingTable), 1),
                ["XX", "XX"],
                new Dictionary<char, Item> { ['X'] = planks }),

            Recipe.Shaped(
                new ItemStack(ItemRegistry.For(BlockRegistry.SandStone), 1),
                ["XX", "XX"],
                new Dictionary<char, Item> { ['X'] = ItemRegistry.For(BlockRegistry.Sand) }),

            Recipe.Shaped(
                new ItemStack(ItemRegistry.For(BlockRegistry.Torch), 4),
                ["C", "S"],
                new Dictionary<char, Item> { ['C'] = ItemRegistry.Coal, ['S'] = stick }),
        ];

        foreach ((ToolKind kind, string[] pattern) in _toolShapes)
        {
            foreach ((ToolMaterial material, Func<Item> bar) in _toolMaterials)
            {
                recipes.Add(Recipe.Shaped(
                    new ItemStack(ToolFor(kind, material), 1),
                    pattern,
                    new Dictionary<char, Item> { ['X'] = bar(), ['S'] = stick }));
            }
        }

        _recipes = [.. recipes];
    }

    public static ItemStack ResultFor(CraftingGrid grid)
    {
        foreach (Recipe recipe in _recipes)
        {
            if (recipe.FitsIn(grid.Size) && recipe.Matches(grid))
            {
                return recipe.Result;
            }
        }

        return ItemStack.Empty;
    }

    private static ToolItem ToolFor(ToolKind kind, ToolMaterial material) => (kind, material) switch
    {
        (ToolKind.Pickaxe, ToolMaterial.Wood) => ItemRegistry.WoodenPickaxe,
        (ToolKind.Pickaxe, ToolMaterial.Stone) => ItemRegistry.StonePickaxe,
        (ToolKind.Pickaxe, ToolMaterial.Iron) => ItemRegistry.IronPickaxe,
        (ToolKind.Pickaxe, ToolMaterial.Gold) => ItemRegistry.GoldenPickaxe,
        (ToolKind.Pickaxe, ToolMaterial.Diamond) => ItemRegistry.DiamondPickaxe,

        (ToolKind.Axe, ToolMaterial.Wood) => ItemRegistry.WoodenAxe,
        (ToolKind.Axe, ToolMaterial.Stone) => ItemRegistry.StoneAxe,
        (ToolKind.Axe, ToolMaterial.Iron) => ItemRegistry.IronAxe,
        (ToolKind.Axe, ToolMaterial.Gold) => ItemRegistry.GoldenAxe,
        (ToolKind.Axe, ToolMaterial.Diamond) => ItemRegistry.DiamondAxe,

        (ToolKind.Shovel, ToolMaterial.Wood) => ItemRegistry.WoodenShovel,
        (ToolKind.Shovel, ToolMaterial.Stone) => ItemRegistry.StoneShovel,
        (ToolKind.Shovel, ToolMaterial.Iron) => ItemRegistry.IronShovel,
        (ToolKind.Shovel, ToolMaterial.Gold) => ItemRegistry.GoldenShovel,
        (ToolKind.Shovel, ToolMaterial.Diamond) => ItemRegistry.DiamondShovel,

        (ToolKind.Sword, ToolMaterial.Wood) => ItemRegistry.WoodenSword,
        (ToolKind.Sword, ToolMaterial.Stone) => ItemRegistry.StoneSword,
        (ToolKind.Sword, ToolMaterial.Iron) => ItemRegistry.IronSword,
        (ToolKind.Sword, ToolMaterial.Gold) => ItemRegistry.GoldenSword,
        (ToolKind.Sword, ToolMaterial.Diamond) => ItemRegistry.DiamondSword,

        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
