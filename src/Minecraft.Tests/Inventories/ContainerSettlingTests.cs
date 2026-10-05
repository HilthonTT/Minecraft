using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class ContainerSettlingTests
{
    [Fact]
    public void TwoPlayersTakingTheSameStackOnlyOneGetsIt()
    {
        var chest = new BlockStateChest();
        chest.SetSlot(0, new ItemStack(ItemRegistry.Diamond, 64));

        ItemStack first = ItemStack.Empty;
        ItemStack second = ItemStack.Empty;

        chest.SetSlot(0, Inventory.ApplyContainerClick(chest, 0, ref first, rightButton: false));
        chest.SetSlot(0, Inventory.ApplyContainerClick(chest, 0, ref second, rightButton: false));

        Assert.Equal(64, first.Count);
        Assert.True(second.IsEmpty);
        Assert.True(chest.GetSlot(0).IsEmpty);
    }

    [Fact]
    public void TwoPlayersFillingTheSameSlotDoNotOverwriteEachOther()
    {
        var chest = new BlockStateChest();

        ItemStack cobblestone = new(BlockRegistry.Cobblestone, 10);
        ItemStack dirt = new(BlockRegistry.Dirt, 5);

        chest.SetSlot(0, Inventory.ApplyContainerClick(chest, 0, ref cobblestone, rightButton: false));
        chest.SetSlot(0, Inventory.ApplyContainerClick(chest, 0, ref dirt, rightButton: false));

        Assert.Equal(ItemRegistry.For(BlockRegistry.Dirt), chest.GetSlot(0).Item);
        Assert.Equal(10, dirt.Count);
        Assert.Equal(ItemRegistry.For(BlockRegistry.Cobblestone), dirt.Item);
    }

    [Fact]
    public void FuelAddedToABurningFurnaceCountsFromWhatIsReallyThere()
    {
        var furnace = new BlockStateFurnace();
        furnace.SetSlot(BlockStateFurnace.FuelSlot, new ItemStack(ItemRegistry.Coal, 2));

        ItemStack cursor = new(ItemRegistry.Coal, 1);
        ItemStack fuel = Inventory.ApplyContainerClick(furnace, BlockStateFurnace.FuelSlot, ref cursor, rightButton: false);

        Assert.Equal(3, fuel.Count);
        Assert.True(cursor.IsEmpty);
    }

    [Fact]
    public void SwappingTheInputStartsTheCookingOver()
    {
        var furnace = new BlockStateFurnace();
        furnace.SetSlot(BlockStateFurnace.InputSlot, new ItemStack(BlockRegistry.Sand, 4));
        furnace.SetSlot(BlockStateFurnace.FuelSlot, new ItemStack(ItemRegistry.Coal, 4));

        for (int i = 0; i < 50; i++)
        {
            furnace.Step();
        }

        Assert.True(furnace.CookTicks > 0);

        furnace.SetSlot(BlockStateFurnace.InputSlot, new ItemStack(BlockRegistry.Cobblestone, 4));

        Assert.Equal(0, furnace.CookTicks);
    }

    [Fact]
    public void AnAcceptedMealIsTakenFromTheHeldStackFirst()
    {
        var inventory = new Inventory();
        inventory.ApplyGameMode(Minecraft.Core.Games.GameMode.Survival);
        inventory.SetSlot(3, new ItemStack(ItemRegistry.Bread, 2));
        inventory.SetSlot(0, new ItemStack(ItemRegistry.Bread, 5));

        Assert.True(inventory.TakeOne(ItemRegistry.Bread));

        Assert.Equal(4, inventory.GetSlot(0).Count);
        Assert.Equal(2, inventory.GetSlot(3).Count);
    }
}
