using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

/// <summary>
/// Ids are what travels over the wire and what is written into a save, so these are the tests that fail when
/// one of them moves. A renumbering is not a refactor: it changes what every existing world says.
/// </summary>
[Collection(RegistryCollection.Name)]
public sealed class ItemRegistryTests
{
    /// <summary>
    /// A spot check of block ids across the whole run of them, oldest and newest. Written out by hand rather
    /// than read from the registry, so that this disagrees with it when one of them is changed.
    /// </summary>
    [Theory]
    [InlineData(1, "Air")]
    [InlineData(2, "Dirt")]
    [InlineData(3, "Stone")]
    [InlineData(17, "Planks")]
    [InlineData(19, "Bedrock")]
    [InlineData(36, "Water")]
    [InlineData(37, "Torch")]
    [InlineData(46, "CraftingTable")]
    public void BlockIdsStayWhereTheyWere(int id, string name)
    {
        Block block = BlockRegistry.GetBlockFromIdentifier(id);

        Assert.Equal(name, NameOfField(block));
    }

    [Fact]
    public void LooseItemIdsStayWhereTheyWere()
    {
        Assert.Equal(256, ItemRegistry.Stick.Id);
        Assert.Equal(257, ItemRegistry.Coal.Id);
        Assert.Equal(258, ItemRegistry.IronIngot.Id);
        Assert.Equal(259, ItemRegistry.GoldIngot.Id);
        Assert.Equal(260, ItemRegistry.Diamond.Id);
        Assert.Equal(261, ItemRegistry.Redstone.Id);
        Assert.Equal(262, ItemRegistry.WoodenPickaxe.Id);
        Assert.Equal(281, ItemRegistry.DiamondSword.Id);
    }

    [Fact]
    public void EveryBlockHasAnItemCarryingItsOwnId()
    {
        for (int id = 1; id <= BlockRegistry.Count; id++)
        {
            Block block = BlockRegistry.GetBlockFromIdentifier(id);
            BlockItem item = ItemRegistry.For(block);

            Assert.Equal(block.Id, item.Id);
            Assert.Equal(block, item.Block);
            Assert.Same(item, ItemRegistry.TryGet(id));
        }
    }

    [Fact]
    public void NothingThatIsNotABlockReachesIntoTheBlockIds()
    {
        Assert.True(BlockRegistry.Count < ItemRegistry.FirstLooseItemId);
        Assert.All(ItemRegistry.LooseItems, item => Assert.True(item.Id >= ItemRegistry.FirstLooseItemId));
    }

    [Fact]
    public void NoTwoItemsShareAnId()
    {
        var seen = new HashSet<ushort>();

        for (int id = 1; id <= BlockRegistry.Count; id++)
        {
            Assert.True(seen.Add(ItemRegistry.For(BlockRegistry.GetBlockFromIdentifier(id)).Id));
        }

        Assert.All(ItemRegistry.LooseItems, item => Assert.True(seen.Add(item.Id)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void AnIdOffTheWireThatMeansNothingComesBackAsNothing(int id)
    {
        // Zero is how an empty hand is sent, and the gap between the blocks and the loose items is unused.
        Assert.Null(ItemRegistry.TryGet(id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void AnUnknownBlockIdIsTurnedDownRatherThanFallenOver(int id)
    {
        Assert.Null(BlockRegistry.TryGetBlockFromIdentifier(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlockRegistry.GetBlockFromIdentifier(id));
    }

    [Fact]
    public void ATooltacksItsOwnLimitsOntoTheStack()
    {
        var stack = new ItemStack(ItemRegistry.DiamondPickaxe, 1);

        Assert.Equal(1, stack.MaxStackSize);
        Assert.Equal(1561, stack.RemainingDurability);
        Assert.Equal(ToolKind.Pickaxe, stack.Tool!.Kind);
        Assert.Equal(ToolMaterial.Diamond, stack.Tool.Material);
    }

    /// <summary>The name of the <see cref="BlockRegistry"/> field holding this block.</summary>
    private static string NameOfField(Block block) =>
        typeof(BlockRegistry)
            .GetFields()
            .Single(field => ReferenceEquals(field.GetValue(null), block))
            .Name;
}
