using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Inventories.Smelting;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Tests.Inventories;

[Collection(RegistryCollection.Name)]
public sealed class SmeltingTests
{
    private static BlockStateFurnace Loaded(ItemStack input, ItemStack fuel)
    {
        var furnace = new BlockStateFurnace();
        furnace.SetSlot(BlockStateFurnace.InputSlot, input);
        furnace.SetSlot(BlockStateFurnace.FuelSlot, fuel);
        return furnace;
    }

    private static void Run(BlockStateFurnace furnace, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            furnace.Step();
        }
    }

    [Fact]
    public void SandBecomesGlassOnceTheCookTimeHasRunOut()
    {
        BlockStateFurnace furnace = Loaded(new ItemStack(BlockRegistry.Sand, 1), new ItemStack(ItemRegistry.Coal, 1));

        Run(furnace, SmeltingRegistry.CookTicks - 1);
        Assert.True(furnace.Output.IsEmpty);
        Assert.True(furnace.IsBurning);

        Run(furnace, 1);
        Assert.Equal(BlockRegistry.Glass, furnace.Output.Block);
        Assert.Equal(1, furnace.Output.Count);
        Assert.True(furnace.Input.IsEmpty);
    }

    [Fact]
    public void ALumpOfCoalSmeltsEightThings()
    {
        BlockStateFurnace furnace = Loaded(
            new ItemStack(BlockRegistry.Cobblestone, 20),
            new ItemStack(ItemRegistry.Coal, 1));

        Run(furnace, SmeltingRegistry.CookTicks * 12);

        Assert.Equal(8, furnace.Output.Count);
        Assert.Equal(BlockRegistry.Stone, furnace.Output.Block);
        Assert.Equal(12, furnace.Input.Count);
        Assert.False(furnace.IsBurning);
    }

    [Fact]
    public void FuelIsNotSpentWhileThereIsNothingToSmelt()
    {
        BlockStateFurnace furnace = Loaded(ItemStack.Empty, new ItemStack(ItemRegistry.Coal, 3));

        Run(furnace, 50);

        Assert.False(furnace.IsBurning);
        Assert.Equal(3, furnace.Fuel.Count);
    }

    [Fact]
    public void SomethingThatDoesNotSmeltIsNeverBurnedFor()
    {
        BlockStateFurnace furnace = Loaded(new ItemStack(BlockRegistry.Dirt, 1), new ItemStack(ItemRegistry.Coal, 1));

        Run(furnace, 50);

        Assert.False(furnace.IsBurning);
        Assert.Equal(1, furnace.Fuel.Count);
    }

    [Fact]
    public void AFullOutputStopsTheFurnace()
    {
        BlockStateFurnace furnace = Loaded(
            new ItemStack(ItemRegistry.RawBeef, 4),
            new ItemStack(ItemRegistry.Coal, 1));
        furnace.SetSlot(BlockStateFurnace.OutputSlot, new ItemStack(ItemRegistry.Steak, ItemStack.MaxCount));

        Run(furnace, SmeltingRegistry.CookTicks * 2);

        Assert.Equal(4, furnace.Input.Count);
        Assert.Equal(1, furnace.Fuel.Count);
    }

    [Fact]
    public void ADifferentThingInTheOutputBlocksTheResult()
    {
        BlockStateFurnace furnace = Loaded(
            new ItemStack(BlockRegistry.IronOre, 1),
            new ItemStack(ItemRegistry.Coal, 1));
        furnace.SetSlot(BlockStateFurnace.OutputSlot, new ItemStack(ItemRegistry.GoldIngot, 1));

        Run(furnace, SmeltingRegistry.CookTicks);

        Assert.Equal(1, furnace.Input.Count);
        Assert.Equal(ItemRegistry.GoldIngot, furnace.Output.Item);
    }

    [Fact]
    public void EveryRawMeatCooks()
    {
        Assert.Equal(ItemRegistry.Steak, SmeltingRegistry.ResultFor(new ItemStack(ItemRegistry.RawBeef, 1)).Item);
        Assert.Equal(
            ItemRegistry.CookedPorkchop,
            SmeltingRegistry.ResultFor(new ItemStack(ItemRegistry.RawPorkchop, 1)).Item);
        Assert.Equal(
            ItemRegistry.CookedMutton,
            SmeltingRegistry.ResultFor(new ItemStack(ItemRegistry.RawMutton, 1)).Item);
    }

    [Fact]
    public void OreSmeltsIntoItsMetal()
    {
        Assert.Equal(ItemRegistry.IronIngot, SmeltingRegistry.ResultFor(new ItemStack(BlockRegistry.IronOre, 1)).Item);
        Assert.Equal(ItemRegistry.GoldIngot, SmeltingRegistry.ResultFor(new ItemStack(BlockRegistry.GoldOre, 1)).Item);
    }

    [Fact]
    public void IronAndGoldComeOutOfTheGroundAsOre()
    {
        Assert.Equal(
            BlockRegistry.IronOre,
            BlockRegistry.IronOre.GetDrop(BlockRegistry.GetState(BlockRegistry.IronOre)).Block);
        Assert.Equal(
            BlockRegistry.GoldOre,
            BlockRegistry.GoldOre.GetDrop(BlockRegistry.GetState(BlockRegistry.GoldOre)).Block);
        Assert.Equal(
            ItemRegistry.Coal,
            BlockRegistry.CoalOre.GetDrop(BlockRegistry.GetState(BlockRegistry.CoalOre)).Item);
    }

    [Fact]
    public void TheFuelSlotTakesOnlyFuelAndTheOutputOnlyGivesBack()
    {
        var furnace = new BlockStateFurnace();

        Assert.True(furnace.Accepts(BlockStateFurnace.FuelSlot, new ItemStack(ItemRegistry.Charcoal, 1)));
        Assert.False(furnace.Accepts(BlockStateFurnace.FuelSlot, new ItemStack(BlockRegistry.Sand, 1)));
        Assert.True(furnace.Accepts(BlockStateFurnace.InputSlot, new ItemStack(BlockRegistry.Dirt, 1)));
        Assert.False(furnace.Accepts(BlockStateFurnace.OutputSlot, new ItemStack(BlockRegistry.Dirt, 1)));
        Assert.True(furnace.IsTakeOnly(BlockStateFurnace.OutputSlot));
    }

    [Fact]
    public void GlassLeavesNothingBehind()
    {
        Assert.True(BlockRegistry.Glass.GetDrop(BlockRegistry.GetState(BlockRegistry.Glass)).IsEmpty);
    }
}
