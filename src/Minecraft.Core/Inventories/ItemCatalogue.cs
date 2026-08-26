using Minecraft.Core.Inventories.Items;

namespace Minecraft.Core.Inventories;

/// <summary>
/// Everything the creative screen hands out, in the order it lays them out.
/// <para>
/// The blocks first, in the order <see cref="BlockCatalogue"/> groups them, and then the things that are not
/// blocks: the materials, and the four tools in each of the five materials they come in. Creative has no use
/// for a tool — a block there comes apart on sight and leaves nothing behind — but the recipes that make one
/// are still worth being able to lay out and look at, and half a supply that stopped at the blocks would be a
/// list of everything with the tools left off it.
/// </para>
/// </summary>
public static class ItemCatalogue
{
    /// <summary>How many things a row of the screen holds, which is also the width of the hotbar.</summary>
    public const int Columns = Inventory.HotbarSlots;

    /// <summary>
    /// Built on first use rather than registered.
    /// <para>
    /// Unlike the registries it is made of, nothing here has to be stable — this is an order to lay slots out
    /// in, not a set of ids that travel over the wire — so there is nothing to be gained by naming a moment
    /// for it to happen at, and something to be lost: a build step that was never called would leave a
    /// screen quietly showing an empty list rather than failing where the mistake was.
    /// </para>
    /// </summary>
    private static readonly Lazy<Item[]> _entries = new(Build);

    public static int Count => _entries.Value.Length;

    /// <summary>How many rows the whole catalogue takes at <see cref="Columns"/> across.</summary>
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
