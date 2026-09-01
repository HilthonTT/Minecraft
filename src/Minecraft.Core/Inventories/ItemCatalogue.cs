using Minecraft.Core.Inventories.Items;

namespace Minecraft.Core.Inventories;

public static class ItemCatalogue
{
    public const int Columns = Inventory.HotbarSlots;

    private static readonly Lazy<Item[]> _entries = new(Build);

    public static int Count => _entries.Value.Length;

    public static int Rows => (Count + Columns - 1) / Columns;

    public static Item ItemAt(int index) => _entries.Value[index];

    private static Item[] Build()
    {
        List<Item> entries = [];

        for (int index = 0; index < BlockCatalogue.Count; index++)
        {
            entries.Add(ItemRegistry.For(BlockCatalogue.BlockAt(index)));
        }

        entries.AddRange(ItemRegistry.LooseItems);
        return [.. entries];
    }
}
