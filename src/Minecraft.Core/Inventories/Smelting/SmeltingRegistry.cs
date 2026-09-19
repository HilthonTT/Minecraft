using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Smelting;

public static class SmeltingRegistry
{
    public const int CookTicks = 200;

    private const int CoalBurnTicks = 1600;
    private const int WoodBurnTicks = 300;
    private const int StickBurnTicks = 100;
    private const int WoodenToolBurnTicks = 200;

    private static readonly Lazy<Dictionary<Item, ItemStack>> _results = new(BuildResults);

    private static readonly Lazy<Dictionary<Item, int>> _burnTicks = new(BuildBurnTicks);

    public static ItemStack ResultFor(ItemStack input) =>
        !input.IsEmpty && _results.Value.TryGetValue(input.Item!, out ItemStack result) ? result : ItemStack.Empty;

    public static int BurnTicksOf(ItemStack fuel) =>
        !fuel.IsEmpty && _burnTicks.Value.TryGetValue(fuel.Item!, out int ticks) ? ticks : 0;

    public static bool IsFuel(ItemStack fuel) => BurnTicksOf(fuel) > 0;

    private static Dictionary<Item, ItemStack> BuildResults()
    {
        return new Dictionary<Item, ItemStack>
        {
            [ItemRegistry.For(BlockRegistry.IronOre)] = new(ItemRegistry.IronIngot, 1),
            [ItemRegistry.For(BlockRegistry.GoldOre)] = new(ItemRegistry.GoldIngot, 1),
            [ItemRegistry.For(BlockRegistry.Sand)] = new(BlockRegistry.Glass, 1),
            [ItemRegistry.For(BlockRegistry.Cobblestone)] = new(BlockRegistry.Stone, 1),
            [ItemRegistry.For(BlockRegistry.OakLog)] = new(ItemRegistry.Charcoal, 1),
            [ItemRegistry.For(BlockRegistry.BirchLog)] = new(ItemRegistry.Charcoal, 1),
            [ItemRegistry.For(BlockRegistry.SpruceLog)] = new(ItemRegistry.Charcoal, 1),
            [ItemRegistry.RawBeef] = new(ItemRegistry.Steak, 1),
            [ItemRegistry.RawPorkchop] = new(ItemRegistry.CookedPorkchop, 1),
            [ItemRegistry.RawMutton] = new(ItemRegistry.CookedMutton, 1),
        };
    }

    private static Dictionary<Item, int> BuildBurnTicks()
    {
        return new Dictionary<Item, int>
        {
            [ItemRegistry.Coal] = CoalBurnTicks,
            [ItemRegistry.Charcoal] = CoalBurnTicks,
            [ItemRegistry.For(BlockRegistry.Planks)] = WoodBurnTicks,
            [ItemRegistry.For(BlockRegistry.OakLog)] = WoodBurnTicks,
            [ItemRegistry.For(BlockRegistry.BirchLog)] = WoodBurnTicks,
            [ItemRegistry.For(BlockRegistry.SpruceLog)] = WoodBurnTicks,
            [ItemRegistry.For(BlockRegistry.CraftingTable)] = WoodBurnTicks,
            [ItemRegistry.For(BlockRegistry.Chest)] = WoodBurnTicks,
            [ItemRegistry.Stick] = StickBurnTicks,
            [ItemRegistry.WoodenPickaxe] = WoodenToolBurnTicks,
            [ItemRegistry.WoodenAxe] = WoodenToolBurnTicks,
            [ItemRegistry.WoodenShovel] = WoodenToolBurnTicks,
            [ItemRegistry.WoodenSword] = WoodenToolBurnTicks,
        };
    }
}
