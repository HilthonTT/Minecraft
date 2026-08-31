using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class CraftingTests
{
    private static Item Planks => ItemRegistry.For(BlockRegistry.Planks);

    /// <summary>Lays a bench out from a picture of it, one character per cell, a dot for an empty one.</summary>
    private static CraftingGrid Lay(int size, string cells, params (char Symbol, Item Item)[] key)
    {
        var grid = new CraftingGrid(size);
        for (int slot = 0; slot < cells.Length; slot++)
        {
            char symbol = cells[slot];
            if (symbol == '.')
            {
                continue;
            }

            grid.SetSlot(slot, new ItemStack(key.Single(entry => entry.Symbol == symbol).Item, 1));
        }

        return grid;
    }

    [Fact]
    public void AnEmptyBenchMakesNothing()
    {
        var grid = new CraftingGrid(2);

        Assert.True(grid.IsEmpty);
        Assert.True(grid.Result.IsEmpty);
    }

    [Fact]
    public void ALogBecomesFourPlanksWhereverItIsPutDown()
    {
        foreach (Block log in new[] { BlockRegistry.OakLog, BlockRegistry.BirchLog, BlockRegistry.SpruceLog })
        {
            CraftingGrid grid = Lay(2, "...L", ('L', ItemRegistry.For(log)));

            Assert.Equal(Planks, grid.Result.Item);
            Assert.Equal(4, grid.Result.Count);
        }
    }

    [Fact]
    public void TwoPlanksOneAboveTheOtherAreFourSticks()
    {
        CraftingGrid grid = Lay(2, "P.P.", ('P', Planks));

        Assert.Equal(ItemRegistry.Stick, grid.Result.Item);
        Assert.Equal(4, grid.Result.Count);
    }

    [Fact]
    public void TwoPlanksSideBySideAreNothing()
    {
        CraftingGrid grid = Lay(2, "PP..", ('P', Planks));

        Assert.True(grid.Result.IsEmpty);
    }

    [Fact]
    public void APatternIsRecognisedAnywhereOnTheBench()
    {
        // The same two planks, in the top left corner of a three by three and then in the bottom right.
        CraftingGrid topLeft = Lay(3, "P..P.....", ('P', Planks));
        CraftingGrid bottomRight = Lay(3, ".....P..P", ('P', Planks));

        Assert.Equal(ItemRegistry.Stick, topLeft.Result.Item);
        Assert.Equal(ItemRegistry.Stick, bottomRight.Result.Item);
    }

    [Fact]
    public void AStrayIngredientBesideARecipeBreaksIt()
    {
        CraftingGrid grid = Lay(3, "P.PP.....", ('P', Planks));

        Assert.True(grid.Result.IsEmpty);
    }

    [Fact]
    public void AToolNeedsTheWholeThreeByThree()
    {
        CraftingGrid bench = Lay(3, "SSS.T..T.", ('S', ItemRegistry.For(BlockRegistry.Cobblestone)), ('T', ItemRegistry.Stick));

        Assert.Equal(ItemRegistry.StonePickaxe, bench.Result.Item);
        Assert.Equal(1, bench.Result.Count);
    }

    [Fact]
    public void ARecipeTooBigForTheBenchIsNeverOffered()
    {
        // The same pickaxe, as far as it will go into a two by two: nothing on that bench can make one.
        CraftingGrid small = Lay(2, "SS.T", ('S', ItemRegistry.For(BlockRegistry.Cobblestone)), ('T', ItemRegistry.Stick));

        Assert.True(small.Result.IsEmpty);
    }

    [Fact]
    public void AnAxeIsAnAxeLaidOutEitherWayRound()
    {
        Item cobble = ItemRegistry.For(BlockRegistry.Cobblestone);
        CraftingGrid right = Lay(3, "XX.XT..T.", ('X', cobble), ('T', ItemRegistry.Stick));
        CraftingGrid left = Lay(3, ".XX.TX.T.", ('X', cobble), ('T', ItemRegistry.Stick));

        Assert.Equal(ItemRegistry.StoneAxe, right.Result.Item);
        Assert.Equal(ItemRegistry.StoneAxe, left.Result.Item);
    }

    [Fact]
    public void TakingTheResultSpendsOneOfEveryCell()
    {
        var grid = new CraftingGrid(2);
        grid.SetSlot(0, new ItemStack(Planks, 3));
        grid.SetSlot(2, new ItemStack(Planks, 3));
        Assert.Equal(ItemRegistry.Stick, grid.Result.Item);

        grid.ConsumeOneOfEach();

        Assert.Equal(2, grid.GetSlot(0).Count);
        Assert.Equal(2, grid.GetSlot(2).Count);
        Assert.Equal(ItemRegistry.Stick, grid.Result.Item);
    }

    [Fact]
    public void SpendingTheLastOfAnIngredientClearsTheResult()
    {
        CraftingGrid grid = Lay(2, "P.P.", ('P', Planks));

        grid.ConsumeOneOfEach();

        Assert.True(grid.IsEmpty);
        Assert.True(grid.Result.IsEmpty);
    }

    [Fact]
    public void ClearingTheBenchHandsBackWhatWasOnIt()
    {
        CraftingGrid grid = Lay(2, "P.P.", ('P', Planks));

        List<ItemStack> taken = grid.TakeAll();

        Assert.Equal(2, taken.Count);
        Assert.All(taken, stack => Assert.Equal(Planks, stack.Item));
        Assert.True(grid.IsEmpty);
        Assert.True(grid.Result.IsEmpty);
    }

    [Fact]
    public void TakingTheResultPutsItOnTheCursorAndSpendsTheBench()
    {
        var inventory = new Inventory();
        inventory.Crafting.SetSlot(0, new ItemStack(Planks, 2));
        inventory.Crafting.SetSlot(2, new ItemStack(Planks, 2));

        inventory.ClickCraftingResult(inventory.Crafting);

        Assert.Equal(ItemRegistry.Stick, inventory.CursorStack.Item);
        Assert.Equal(4, inventory.CursorStack.Count);
        Assert.Equal(1, inventory.Crafting.GetSlot(0).Count);
    }

    [Fact]
    public void HoldingSomethingElseRefusesTheResultRatherThanThrowingItAway()
    {
        var inventory = new Inventory();
        inventory.Crafting.SetSlot(0, new ItemStack(Planks, 1));
        inventory.Crafting.SetSlot(2, new ItemStack(Planks, 1));
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 4));
        inventory.ClickSlot(0, rightButton: false);

        inventory.ClickCraftingResult(inventory.Crafting);

        Assert.Equal(BlockRegistry.Dirt, inventory.CursorStack.Block);
        Assert.Equal(1, inventory.Crafting.GetSlot(0).Count);
    }

    [Fact]
    public void ClosingTheScreenReturnsTheIngredients()
    {
        var inventory = new Inventory();
        inventory.ApplyGameMode(Minecraft.Core.Games.GameMode.Survival);
        inventory.Crafting.SetSlot(0, new ItemStack(Planks, 3));

        inventory.ReturnCraftingGrid(inventory.Crafting);

        Assert.True(inventory.Crafting.IsEmpty);
        Assert.Equal(3, inventory.GetSlot(0).Count);
        Assert.Equal(Planks, inventory.GetSlot(0).Item);
    }
}
