using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;
using Minecraft.Core.Worlds.Lighting;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class LavaTests
{
    [Fact]
    public void LavaThinsOutOverFewerCellsThanWater()
    {
        var lava = (BlockLava)BlockRegistry.Lava;
        var water = (BlockWater)BlockRegistry.Water;

        Assert.True(lava.MaxLevel < water.MaxLevel);
        Assert.Equal(BlockRegistry.LavaFlowing3, lava.GetForLevel(lava.MaxLevel));
        Assert.Equal(BlockRegistry.Air, lava.GetForLevel(lava.MaxLevel + 1));
    }

    [Fact]
    public void LavaAndWaterAreDifferentFluids()
    {
        var lava = (BlockFluid)BlockRegistry.LavaFlowing2;

        Assert.True(lava.IsSameFluidAs(BlockRegistry.Lava));
        Assert.False(lava.IsSameFluidAs(BlockRegistry.Water));
        Assert.False(lava.IsSameFluidAs(BlockRegistry.Stone));
    }

    [Fact]
    public void EveryLevelOfLavaGivesOffLight()
    {
        foreach (Block level in new[]
                 {
                     BlockRegistry.Lava, BlockRegistry.LavaFalling, BlockRegistry.LavaFlowing1,
                     BlockRegistry.LavaFlowing2, BlockRegistry.LavaFlowing3,
                 })
        {
            Assert.IsAssignableFrom<ILightSource>(BlockRegistry.GetState(level));
        }
    }

    [Fact]
    public void LavaIsDrawnWithTheSolidBlocksAndWaterIsNot()
    {
        Assert.False(BlockRegistry.Lava.IsTranslucent);
        Assert.True(BlockRegistry.Water.IsTranslucent);
        Assert.True(BlockRegistry.Lava.IsLiquid);
    }

    [Fact]
    public void LavaCannotBePickedBackUp()
    {
        Assert.True(BlockRegistry.Lava.GetDrop(BlockRegistry.GetState(BlockRegistry.Lava)).IsEmpty);
    }

    [Fact]
    public void OnlyADiamondPickaxeGetsObsidianOut()
    {
        Assert.False(Harvesting.CanHarvest(BlockRegistry.Obsidian, new ItemStack(ItemRegistry.IronPickaxe, 1)));
        Assert.True(Harvesting.CanHarvest(BlockRegistry.Obsidian, new ItemStack(ItemRegistry.DiamondPickaxe, 1)));
    }
}
