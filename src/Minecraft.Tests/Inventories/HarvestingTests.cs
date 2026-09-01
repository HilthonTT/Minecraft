using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class HarvestingTests
{
    private static readonly ItemStack BareHand = ItemStack.Empty;

    [Fact]
    public void TheRightToolDividesTheTimeByItsSpeed()
    {
        float bare = Harvesting.SecondsToBreak(BlockRegistry.Stone, BareHand);
        float wooden = Harvesting.SecondsToBreak(BlockRegistry.Stone, new ItemStack(ItemRegistry.WoodenPickaxe, 1));
        float diamond = Harvesting.SecondsToBreak(BlockRegistry.Stone, new ItemStack(ItemRegistry.DiamondPickaxe, 1));

        Assert.Equal(BlockRegistry.Stone.SecondsToBreak / 2F, wooden, 4);
        Assert.Equal(BlockRegistry.Stone.SecondsToBreak / 8F, diamond, 4);
        Assert.True(diamond < wooden && wooden < bare);
    }

    [Fact]
    public void GoldDigsFasterThanDiamondAndLastsFarLess()
    {
        float gold = Harvesting.SecondsToBreak(BlockRegistry.Stone, new ItemStack(ItemRegistry.GoldenPickaxe, 1));
        float diamond = Harvesting.SecondsToBreak(BlockRegistry.Stone, new ItemStack(ItemRegistry.DiamondPickaxe, 1));

        Assert.True(gold < diamond);
        Assert.True(ItemRegistry.GoldenPickaxe.MaxDurability < ItemRegistry.WoodenPickaxe.MaxDurability);
    }

    [Fact]
    public void ABlockThatWantsATooltakesLongerWithoutOne()
    {
        float bare = Harvesting.SecondsToBreak(BlockRegistry.Stone, BareHand);
        float shovel = Harvesting.SecondsToBreak(BlockRegistry.Stone, new ItemStack(ItemRegistry.DiamondShovel, 1));

        Assert.Equal(bare, shovel, 4);
        Assert.True(bare > BlockRegistry.Stone.SecondsToBreak);
    }

    [Fact]
    public void ABlockThatAsksForNoToolIsNoSlowerBareHanded()
    {
        float bare = Harvesting.SecondsToBreak(BlockRegistry.Dirt, BareHand);

        Assert.Equal(BlockRegistry.Dirt.SecondsToBreak, bare, 4);
    }

    [Fact]
    public void NothingGetsThroughBedrock()
    {
        Assert.False(BlockRegistry.Bedrock.IsBreakable);
        Assert.Equal(
            float.PositiveInfinity,
            Harvesting.SecondsToBreak(BlockRegistry.Bedrock, new ItemStack(ItemRegistry.DiamondPickaxe, 1)));
    }

    [Fact]
    public void AnythingThatAsksForNoToolDropsWhateverIsHeld()
    {
        Assert.True(Harvesting.CanHarvest(BlockRegistry.Dirt, BareHand));
        Assert.True(Harvesting.CanHarvest(BlockRegistry.Dirt, new ItemStack(ItemRegistry.WoodenSword, 1)));
    }

    [Fact]
    public void StoneNeedsAPickaxeOfAnyMaterialAtAll()
    {
        Assert.False(Harvesting.CanHarvest(BlockRegistry.Stone, BareHand));
        Assert.False(Harvesting.CanHarvest(BlockRegistry.Stone, new ItemStack(ItemRegistry.DiamondAxe, 1)));
        Assert.True(Harvesting.CanHarvest(BlockRegistry.Stone, new ItemStack(ItemRegistry.WoodenPickaxe, 1)));
    }

    [Fact]
    public void SomethingBuriedDeeperNeedsAToolThatReachesIt()
    {
        Assert.False(Harvesting.CanHarvest(BlockRegistry.IronOre, new ItemStack(ItemRegistry.WoodenPickaxe, 1)));
        Assert.False(Harvesting.CanHarvest(BlockRegistry.IronOre, new ItemStack(ItemRegistry.GoldenPickaxe, 1)));
        Assert.True(Harvesting.CanHarvest(BlockRegistry.IronOre, new ItemStack(ItemRegistry.StonePickaxe, 1)));

        Assert.False(Harvesting.CanHarvest(BlockRegistry.DiamondOre, new ItemStack(ItemRegistry.StonePickaxe, 1)));
        Assert.True(Harvesting.CanHarvest(BlockRegistry.DiamondOre, new ItemStack(ItemRegistry.IronPickaxe, 1)));
    }

    [Fact]
    public void ASwordIsTheRightToolForNothingAtAll()
    {
        foreach (int id in Enumerable.Range(1, BlockRegistry.Count))
        {
            Block block = BlockRegistry.GetBlockFromIdentifier(id);

            Assert.False(Harvesting.IsCorrectToolFor(block, new ItemStack(ItemRegistry.DiamondSword, 1)));
        }
    }

    [Fact]
    public void ASwordIsStillWhatHitsHardest()
    {
        Assert.True(ItemRegistry.DiamondSword.AttackDamage > ItemRegistry.DiamondAxe.AttackDamage);
        Assert.True(ItemRegistry.DiamondAxe.AttackDamage > ItemRegistry.DiamondPickaxe.AttackDamage);
        Assert.True(ItemRegistry.DiamondPickaxe.AttackDamage > ItemRegistry.DiamondShovel.AttackDamage);
        Assert.True(ItemRegistry.WoodenShovel.AttackDamage >= 1);
    }
}
