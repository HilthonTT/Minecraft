using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.IO;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class ContainerStateTests
{
    private static T RoundTrip<T>(T state) where T : BlockState
    {
        using var memory = new MemoryStream();
        using (var buffered = new BufferedStream(memory))
        {
            var writer = new BufferedDataStream(buffered);
            state.ToStream(writer);
            writer.Flush();
        }

        byte[] bytes = memory.ToArray();
        int head = 0;

        ushort blockId = DataConverter.BytesToUInt16(bytes, ref head);
        BlockState read = BlockRegistry.GetState(BlockRegistry.GetBlockFromIdentifier(blockId));
        read.ExtractFromByteStream(bytes, ref head);

        Assert.Equal(bytes.Length, head);
        Assert.Equal(state.PayloadSize(), bytes.Length - sizeof(ushort));
        return Assert.IsType<T>(read);
    }

    [Fact]
    public void AChestIsSavedWithEverythingInIt()
    {
        var chest = new BlockStateChest { Facing = Direction.Left };
        chest.SetSlot(0, new ItemStack(BlockRegistry.Cobblestone, 64));
        chest.SetSlot(13, new ItemStack(ItemRegistry.IronPickaxe, 1, damage: 40));
        chest.SetSlot(26, new ItemStack(ItemRegistry.Steak, 5));

        BlockStateChest read = RoundTrip(chest);

        Assert.Equal(Direction.Left, read.Facing);
        Assert.True(read.GetSlot(0).SameAs(chest.GetSlot(0)));
        Assert.True(read.GetSlot(13).SameAs(chest.GetSlot(13)));
        Assert.Equal(40, read.GetSlot(13).Damage);
        Assert.True(read.GetSlot(26).SameAs(chest.GetSlot(26)));
        Assert.True(read.GetSlot(1).IsEmpty);
    }

    [Fact]
    public void AFurnaceIsSavedMidBurn()
    {
        var furnace = new BlockStateFurnace { Facing = Direction.Back };
        furnace.SetSlot(BlockStateFurnace.InputSlot, new ItemStack(BlockRegistry.Sand, 10));
        furnace.SetSlot(BlockStateFurnace.FuelSlot, new ItemStack(ItemRegistry.Coal, 2));

        for (int tick = 0; tick < 30; tick++)
        {
            furnace.Step();
        }

        BlockStateFurnace read = RoundTrip(furnace);

        Assert.Equal(Direction.Back, read.Facing);
        Assert.True(read.IsBurning);
        Assert.Equal(furnace.BurnTicksRemaining, read.BurnTicksRemaining);
        Assert.Equal(furnace.CookTicks, read.CookTicks);
        Assert.True(read.Fuel.SameAs(furnace.Fuel));
        Assert.True(read.Input.SameAs(furnace.Input));
        Assert.Equal(1, read.Appearance);
    }

    [Fact]
    public void AnUnknownItemInASavedSlotComesBackEmpty()
    {
        ItemStack stack = ItemStackCodec.FromParts(itemId: 60000, count: 3, damage: 0);

        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void CopyingAChestTakesItsContentsButNotItsFacing()
    {
        var source = new BlockStateChest { Facing = Direction.Right };
        source.SetSlot(4, new ItemStack(ItemRegistry.Bread, 3));

        var target = new BlockStateChest { Facing = Direction.Front };
        target.CopyContentsFrom(source);

        Assert.True(target.GetSlot(4).SameAs(source.GetSlot(4)));
        Assert.Equal(Direction.Front, target.Facing);
    }
}
