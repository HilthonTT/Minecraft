using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Tests.Entities;

[Collection(RegistryCollection.Name)]
public sealed class MobSpawnerTests
{
    private const int Ground = 60;

    private static Chunk ColumnOver(int ground, params (int Y, Block Block)[] above)
    {
        var chunk = new Chunk(0, 0);
        chunk.AddBlockAt(8, ground, 8, BlockRegistry.GetState(BlockRegistry.Grass));

        foreach ((int y, Block block) in above)
        {
            chunk.AddBlockAt(8, y, 8, BlockRegistry.GetState(block));
        }

        return chunk;
    }

    [Fact]
    public void TheSunReachesTheGroundUnderATree()
    {
        // A canopy nine blocks up, two layers thick, is shade rather than a roof.
        Chunk chunk = ColumnOver(Ground, (Ground + 9, BlockRegistry.OakLeaves), (Ground + 10, BlockRegistry.OakLeaves));

        Assert.Equal(Ground + 1, MobSpawner.SunlitFloorOfColumn(chunk, 8, 8));
    }

    [Fact]
    public void RockOverheadStillRoofsACave()
    {
        Chunk chunk = ColumnOver(Ground, (Ground + 9, BlockRegistry.Stone));

        Assert.Equal(Ground + 10, MobSpawner.SunlitFloorOfColumn(chunk, 8, 8));
    }

    [Fact]
    public void ALogIsSolidEvenWhereLeavesAreNot()
    {
        Chunk chunk = ColumnOver(
            Ground,
            (Ground + 1, BlockRegistry.OakLog),
            (Ground + 2, BlockRegistry.OakLog),
            (Ground + 3, BlockRegistry.OakLeaves));

        Assert.Equal(Ground + 3, MobSpawner.SunlitFloorOfColumn(chunk, 8, 8));
    }
}
