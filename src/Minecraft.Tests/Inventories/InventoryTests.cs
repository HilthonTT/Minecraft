using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class InventoryTests
{
    /// <summary>An inventory in survival, which is the mode where anything is actually spent.</summary>
    private static Inventory Survival()
    {
        var inventory = new Inventory();
        inventory.ApplyGameMode(GameMode.Survival);
        return inventory;
    }

    [Fact]
    public void CreativeOpensOnTheStartingPalette()
    {
        var inventory = new Inventory();

        Assert.Equal(GameMode.Creative, inventory.GameMode);
        for (int slot = 0; slot < Inventory.HotbarSlots && slot < BlockPalette.Blocks.Count; slot++)
        {
            Assert.Equal(BlockPalette.Blocks[slot], inventory.GetSlot(slot).Block);
            Assert.Equal(ItemStack.MaxCount, inventory.GetSlot(slot).Count);
        }
    }

    [Fact]
    public void SurvivalOpensEmpty()
    {
        Inventory inventory = Survival();

        for (int slot = 0; slot < Inventory.TotalSlots; slot++)
        {
            Assert.True(inventory.GetSlot(slot).IsEmpty);
        }
    }

    [Fact]
    public void MovingIntoSurvivalEmptiesWhatCreativeHandedOut()
    {
        var inventory = new Inventory();
        Assert.False(inventory.Selected.IsEmpty);

        inventory.ApplyGameMode(GameMode.Survival);

        Assert.True(inventory.Selected.IsEmpty);
    }

    [Fact]
    public void TheHotbarSelectionWrapsBothWays()
    {
        Inventory inventory = Survival();

        inventory.StepHotbarSelection(-1);
        Assert.Equal(Inventory.HotbarSlots - 1, inventory.SelectedHotbarSlot);

        inventory.StepHotbarSelection(1);
        Assert.Equal(0, inventory.SelectedHotbarSlot);

        inventory.StepHotbarSelection(Inventory.HotbarSlots + 2);
        Assert.Equal(2, inventory.SelectedHotbarSlot);
    }

    [Fact]
    public void OnlyTheNineCanBeSelected()
    {
        Inventory inventory = Survival();

        inventory.SelectHotbarSlot(Inventory.HotbarSlots);
        inventory.SelectHotbarSlot(-1);

        Assert.Equal(0, inventory.SelectedHotbarSlot);
    }

    [Fact]
    public void AddingTopsUpAPileBeforeOpeningANewOne()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 60));

        ItemStack leftOver = inventory.TryAdd(new ItemStack(BlockRegistry.Dirt, 10));

        Assert.True(leftOver.IsEmpty);
        Assert.Equal(ItemStack.MaxCount, inventory.GetSlot(0).Count);
        Assert.Equal(6, inventory.GetSlot(1).Count);
    }

    [Fact]
    public void WhatWillNotFitComesBack()
    {
        Inventory inventory = Survival();
        for (int slot = 0; slot < Inventory.TotalSlots; slot++)
        {
            inventory.SetSlot(slot, new ItemStack(BlockRegistry.Stone, ItemStack.MaxCount));
        }

        ItemStack leftOver = inventory.TryAdd(new ItemStack(BlockRegistry.Dirt, 5));

        Assert.Equal(5, leftOver.Count);
        Assert.Equal(BlockRegistry.Dirt, leftOver.Block);
    }

    [Fact]
    public void AddingNothingChangesNothing()
    {
        Inventory inventory = Survival();

        Assert.True(inventory.TryAdd(ItemStack.Empty).IsEmpty);
        Assert.True(inventory.GetSlot(0).IsEmpty);
    }

    [Fact]
    public void ToolsNeverPileUp()
    {
        Inventory inventory = Survival();

        inventory.TryAdd(new ItemStack(ItemRegistry.IronPickaxe, 1));
        inventory.TryAdd(new ItemStack(ItemRegistry.IronPickaxe, 1));

        Assert.Equal(1, inventory.GetSlot(0).Count);
        Assert.Equal(1, inventory.GetSlot(1).Count);
    }

    [Fact]
    public void PlacingABlockSpendsOneInSurvivalAndNothingInCreative()
    {
        Inventory survival = Survival();
        survival.SetSlot(0, new ItemStack(BlockRegistry.Stone, 2));

        Assert.True(survival.TryConsumeSelected());
        Assert.Equal(1, survival.Selected.Count);
        Assert.True(survival.TryConsumeSelected());
        Assert.True(survival.Selected.IsEmpty);
        Assert.False(survival.TryConsumeSelected());

        var creative = new Inventory();
        int before = creative.Selected.Count;
        Assert.True(creative.TryConsumeSelected());
        Assert.Equal(before, creative.Selected.Count);
    }

    [Fact]
    public void ThrowingSomethingDownReallyEmptiesTheSlotInEitherMode()
    {
        var creative = new Inventory();
        int before = creative.Selected.Count;

        ItemStack thrown = creative.TakeFromSelected(10);

        Assert.Equal(10, thrown.Count);
        Assert.Equal(before - 10, creative.Selected.Count);
    }

    [Fact]
    public void ThrowingTakesNoMoreThanIsThere()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Sand, 3));

        Assert.Equal(3, inventory.TakeFromSelected(10).Count);
        Assert.True(inventory.Selected.IsEmpty);
        Assert.True(inventory.TakeFromSelected(1).IsEmpty);
    }

    [Fact]
    public void AToolTakesWearInSurvivalAndReportsTheSwingThatBrokeIt()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(ItemRegistry.WoodenPickaxe, 1, damage: 57));

        Assert.False(inventory.WearSelected());
        Assert.Equal(58, inventory.Selected.Damage);

        Assert.True(inventory.WearSelected());
        Assert.True(inventory.Selected.IsEmpty);
    }

    [Fact]
    public void NothingWearsOutInCreative()
    {
        var inventory = new Inventory();
        inventory.SetSlot(0, new ItemStack(ItemRegistry.WoodenPickaxe, 1));

        Assert.False(inventory.WearSelected());
        Assert.Equal(0, inventory.Selected.Damage);
    }

    [Fact]
    public void PickingABlockSelectsASlotAlreadyHoldingIt()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(4, new ItemStack(BlockRegistry.Gravel, 1));

        inventory.PickBlock(BlockRegistry.Gravel);

        Assert.Equal(4, inventory.SelectedHotbarSlot);
    }

    [Fact]
    public void PickingABlockConjuresNothingInSurvival()
    {
        Inventory inventory = Survival();

        inventory.PickBlock(BlockRegistry.Gravel);

        Assert.True(inventory.Selected.IsEmpty);
    }

    [Fact]
    public void PickingABlockFillsTheHandInCreative()
    {
        var inventory = new Inventory();

        inventory.PickBlock(BlockRegistry.Bedrock);

        Assert.Equal(BlockRegistry.Bedrock, inventory.Selected.Block);
    }

    [Fact]
    public void LeftClickingLiftsAStackAndPutsItDownAgain()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 20));

        inventory.ClickSlot(0, rightButton: false);
        Assert.Equal(20, inventory.CursorStack.Count);
        Assert.True(inventory.GetSlot(0).IsEmpty);

        inventory.ClickSlot(5, rightButton: false);
        Assert.True(inventory.CursorStack.IsEmpty);
        Assert.Equal(20, inventory.GetSlot(5).Count);
    }

    [Fact]
    public void RightClickingSplitsAPileAndKeepsTheLargerHalf()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 5));

        inventory.ClickSlot(0, rightButton: true);

        Assert.Equal(3, inventory.CursorStack.Count);
        Assert.Equal(2, inventory.GetSlot(0).Count);
    }

    [Fact]
    public void RightClickingPutsASingleBlockDown()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 4));
        inventory.ClickSlot(0, rightButton: false);

        inventory.ClickSlot(9, rightButton: true);

        Assert.Equal(3, inventory.CursorStack.Count);
        Assert.Equal(1, inventory.GetSlot(9).Count);
    }

    [Fact]
    public void TwoDifferentBlocksTradePlaces()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 4));
        inventory.SetSlot(1, new ItemStack(BlockRegistry.Sand, 7));
        inventory.ClickSlot(0, rightButton: false);

        inventory.ClickSlot(1, rightButton: false);

        Assert.Equal(BlockRegistry.Sand, inventory.CursorStack.Block);
        Assert.Equal(7, inventory.CursorStack.Count);
        Assert.Equal(BlockRegistry.Dirt, inventory.GetSlot(1).Block);
    }

    [Fact]
    public void PouringOntoAFullSlotLeavesTheRestOnTheCursor()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 60));
        inventory.SetSlot(1, new ItemStack(BlockRegistry.Dirt, 10));
        inventory.ClickSlot(1, rightButton: false);

        inventory.ClickSlot(0, rightButton: false);

        Assert.Equal(ItemStack.MaxCount, inventory.GetSlot(0).Count);
        Assert.Equal(6, inventory.CursorStack.Count);
    }

    [Fact]
    public void ClosingTheScreenPutsTheCursorBack()
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 4));
        inventory.ClickSlot(0, rightButton: false);

        inventory.ReturnCursorStack();

        Assert.True(inventory.CursorStack.IsEmpty);
        Assert.Equal(4, inventory.GetSlot(0).Count);
    }

    [Fact]
    public void ChangesAreAnnouncedOnce()
    {
        Inventory inventory = Survival();
        int changes = 0;
        inventory.OnChangedHandler += () => changes++;

        inventory.SetSlot(0, new ItemStack(BlockRegistry.Dirt, 1));

        Assert.Equal(1, changes);
    }
}
