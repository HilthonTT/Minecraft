using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class ItemStackTests
{
    [Fact]
    public void DefaultStackIsEmpty()
    {
        ItemStack stack = default;

        Assert.True(stack.IsEmpty);
        Assert.Null(stack.Item);
        Assert.Equal(0, stack.Count);
        Assert.True(ItemStack.Empty.IsEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AStackOfNothingIsTheEmptyStack(int count)
    {
        var stack = new ItemStack(ItemRegistry.Stick, count);

        Assert.True(stack.IsEmpty);
        Assert.Null(stack.Item);
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void CountIsCappedAtWhatTheItemStacksTo()
    {
        var sticks = new ItemStack(ItemRegistry.Stick, 500);
        var pickaxe = new ItemStack(ItemRegistry.IronPickaxe, 64);

        Assert.Equal(ItemStack.MaxCount, sticks.Count);
        Assert.Equal(1, pickaxe.Count);
    }

    [Fact]
    public void OnlyADamageableItemCarriesWear()
    {
        var worn = new ItemStack(ItemRegistry.WoodenPickaxe, 1, damage: 10);
        var sticks = new ItemStack(ItemRegistry.Stick, 1, damage: 10);

        Assert.Equal(10, worn.Damage);
        Assert.Equal(0, sticks.Damage);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(int.MaxValue, 59)]
    public void WearIsClampedToWhatTheItemHasInIt(int damage, int expected)
    {
        var stack = new ItemStack(ItemRegistry.WoodenPickaxe, 1, damage);

        Assert.Equal(expected, stack.Damage);
    }

    [Fact]
    public void WearingATooltakesOneSwingOffIt()
    {
        var pickaxe = new ItemStack(ItemRegistry.WoodenPickaxe, 1);

        ItemStack worn = pickaxe.Worn();

        Assert.Equal(1, worn.Damage);
        Assert.Equal(58, worn.RemainingDurability);
    }

    [Fact]
    public void TheLastSwingLeavesNothingBehind()
    {
        var almostGone = new ItemStack(ItemRegistry.WoodenPickaxe, 1, damage: 58);

        Assert.True(almostGone.Worn().IsEmpty);
    }

    [Fact]
    public void NothingThatDoesNotWearOutIsWornDown()
    {
        var sticks = new ItemStack(ItemRegistry.Stick, 4);

        Assert.Equal(4, sticks.Worn().Count);
        Assert.Equal(0, sticks.Worn().Damage);
    }

    [Fact]
    public void ChangingTheCountKeepsTheWear()
    {
        var pickaxe = new ItemStack(ItemRegistry.IronPickaxe, 1, damage: 30);

        Assert.Equal(30, pickaxe.WithCount(1).Damage);
        Assert.True(pickaxe.WithCount(0).IsEmpty);
    }

    [Fact]
    public void PilesOfTheSameThingPourTogether()
    {
        var a = new ItemStack(ItemRegistry.Coal, 10);
        var b = new ItemStack(ItemRegistry.Coal, 5);

        Assert.True(a.CanStackWith(b));
        Assert.Equal(ItemStack.MaxCount - 10, a.RemainingSpace);
    }

    [Fact]
    public void NothingElsePoursTogether()
    {
        var coal = new ItemStack(ItemRegistry.Coal, 10);
        var diamond = new ItemStack(ItemRegistry.Diamond, 10);
        var pickaxe = new ItemStack(ItemRegistry.IronPickaxe, 1);
        var otherPickaxe = new ItemStack(ItemRegistry.IronPickaxe, 1);

        Assert.False(coal.CanStackWith(diamond));
        Assert.False(coal.CanStackWith(ItemStack.Empty));
        Assert.False(ItemStack.Empty.CanStackWith(coal));
        Assert.False(ItemStack.Empty.CanStackWith(ItemStack.Empty));

        Assert.False(pickaxe.CanStackWith(otherPickaxe));
    }

    [Fact]
    public void AStackOfABlockKnowsWhatItPutsDown()
    {
        var stone = new ItemStack(BlockRegistry.Stone, 1);

        Assert.Equal(BlockRegistry.Stone, stone.Block);
        Assert.Equal(ItemRegistry.For(BlockRegistry.Stone), stone.Item);
        Assert.Null(new ItemStack(ItemRegistry.Stick, 1).Block);
        Assert.Null(ItemStack.Empty.Block);
    }

    [Fact]
    public void OnlyAToolReadsAsOne()
    {
        Assert.NotNull(new ItemStack(ItemRegistry.DiamondSword, 1).Tool);
        Assert.Null(new ItemStack(ItemRegistry.Stick, 1).Tool);
        Assert.Null(new ItemStack(BlockRegistry.Stone, 1).Tool);
    }
}
