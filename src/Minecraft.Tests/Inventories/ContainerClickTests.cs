using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class ContainerClickTests
{
    private static Inventory Survival()
    {
        var inventory = new Inventory();
        inventory.ApplyGameMode(GameMode.Survival);
        return inventory;
    }

    private static Inventory HoldingOnCursor(ItemStack stack)
    {
        Inventory inventory = Survival();
        inventory.SetSlot(0, stack);
        inventory.ClickSlot(0, rightButton: false);
        return inventory;
    }

    [Fact]
    public void AnEmptyCursorPicksUpTheWholeSlot()
    {
        Inventory inventory = Survival();

        ItemStack left = inventory.ClickExternalSlot(new ItemStack(BlockRegistry.Dirt, 10), rightButton: false);

        Assert.True(left.IsEmpty);
        Assert.Equal(10, inventory.CursorStack.Count);
    }

    [Fact]
    public void ARightClickPicksUpHalfRoundedUp()
    {
        Inventory inventory = Survival();

        ItemStack left = inventory.ClickExternalSlot(new ItemStack(BlockRegistry.Dirt, 7), rightButton: true);

        Assert.Equal(3, left.Count);
        Assert.Equal(4, inventory.CursorStack.Count);
    }

    [Fact]
    public void ASlotThatRefusesTheCursorIsLeftAlone()
    {
        Inventory inventory = HoldingOnCursor(new ItemStack(BlockRegistry.Sand, 5));

        ItemStack slot = inventory.ClickExternalSlot(ItemStack.Empty, rightButton: false, accepts: _ => false);

        Assert.True(slot.IsEmpty);
        Assert.Equal(5, inventory.CursorStack.Count);
    }

    [Fact]
    public void TheCursorPoursIntoAMatchingSlot()
    {
        Inventory inventory = HoldingOnCursor(new ItemStack(ItemRegistry.Coal, 10));

        ItemStack slot = inventory.ClickExternalSlot(new ItemStack(ItemRegistry.Coal, 60), rightButton: false);

        Assert.Equal(64, slot.Count);
        Assert.Equal(6, inventory.CursorStack.Count);
    }

    [Fact]
    public void AnOutputSlotCanOnlyBeTakenFrom()
    {
        Inventory inventory = HoldingOnCursor(new ItemStack(ItemRegistry.IronIngot, 3));

        ItemStack slot = inventory.ClickTakeOnlySlot(new ItemStack(ItemRegistry.IronIngot, 2));

        Assert.True(slot.IsEmpty);
        Assert.Equal(5, inventory.CursorStack.Count);
    }

    [Fact]
    public void AnOutputSlotIsNotSwappedWithADifferentCursor()
    {
        Inventory inventory = HoldingOnCursor(new ItemStack(BlockRegistry.Dirt, 3));

        ItemStack slot = inventory.ClickTakeOnlySlot(new ItemStack(ItemRegistry.IronIngot, 2));

        Assert.Equal(2, slot.Count);
        Assert.Equal(BlockRegistry.Dirt, inventory.CursorStack.Block);
    }
}
