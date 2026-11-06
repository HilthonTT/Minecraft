using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class ChunkLightingTests
{
    private const int GridSize = 4;
    private const int FloorY = 60;

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void ChunksLitAsTheyLoadMatchLightingTheWholeWorldAtOnce(int seed)
    {
        var expected = new LightingWorld();
        var actual = new LightingWorld();

        Chunk[] expectedChunks = BuildChunks(seed);
        Chunk[] actualChunks = BuildChunks(seed);

        LightAllAtOnce(expected, expectedChunks);

        foreach (int index in LoadOrder(seed))
        {
            LoadInOnePass(actual, actualChunks[index]);
        }

        for (int index = 0; index < expectedChunks.Length; index++)
        {
            AssertSameBlockLight(expectedChunks[index], actualChunks[index]);
        }
    }

    [Fact]
    public void LightFromANeighbourSpillsIntoAChunkLoadedAfterIt()
    {
        var world = new LightingWorld();
        var lit = new Chunk(0, 0);
        var dark = new Chunk(1, 0);

        lit.AddBlockAt(15, FloorY, 8, BlockRegistry.GetState(BlockRegistry.Glowstone));

        LoadInOnePass(world, lit);
        LoadInOnePass(world, dark);

        LightChannel channel = LightUtils.BlockVisibileColorChannels.First();
        uint atSource = LightUtils.GetLightOfChannel(lit, new Vector3i(15, FloorY, 8), channel);
        uint acrossTheBorder = LightUtils.GetLightOfChannel(dark, new Vector3i(0, FloorY, 8), channel);

        Assert.True(atSource > 1);
        Assert.Equal(atSource - 1, acrossTheBorder);
    }

    private static void LightAllAtOnce(World world, Chunk[] chunks)
    {
        foreach (Chunk chunk in chunks)
        {
            world.AddPlayerPresenceToChunk(chunk);
        }

        foreach (Chunk chunk in chunks)
        {
            foreach (KeyValuePair<Vector3i, BlockState> source in chunk.LightSourceBlocks.ToArray())
            {
                BlockLightPropagation.RepairOnBlockAdded(world, chunk, source.Key, source.Value);
            }
        }
    }

    private static void LoadInOnePass(World world, Chunk chunk)
    {
        world.AddPlayerPresenceToChunk(chunk);
        SunlightPropagation.GenerateInitialGrid(world, chunk);
        BlockLightPropagation.LightLoadedChunk(world, chunk);
    }

    private static Chunk[] BuildChunks(int seed)
    {
        var random = new Random(seed);
        BlockState stone = BlockRegistry.GetState(BlockRegistry.Stone);
        BlockState[] sources =
        [
            BlockRegistry.GetState(BlockRegistry.Glowstone),
            BlockRegistry.GetState(BlockRegistry.Torch),
            BlockRegistry.GetState(BlockRegistry.Lava),
        ];

        var chunks = new Chunk[GridSize * GridSize];
        for (int gridX = 0; gridX < GridSize; gridX++)
        {
            for (int gridZ = 0; gridZ < GridSize; gridZ++)
            {
                var chunk = new Chunk(gridX, gridZ);

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.AddBlockAt(x, FloorY - 1, z, stone);
                        for (int y = FloorY + 6; y < FloorY + 9; y++)
                        {
                            chunk.AddBlockAt(x, y, z, stone);
                        }
                    }
                }

                for (int wall = 0; wall < 40; wall++)
                {
                    chunk.AddBlockAt(random.Next(16), FloorY + random.Next(6), random.Next(16), stone);
                }

                for (int light = 0; light < 3; light++)
                {
                    chunk.AddBlockAt(
                        random.Next(16),
                        FloorY + random.Next(6),
                        random.Next(16),
                        sources[random.Next(sources.Length)]);
                }

                chunks[(gridX * GridSize) + gridZ] = chunk;
            }
        }

        return chunks;
    }

    private static int[] LoadOrder(int seed)
    {
        int[] order = Enumerable.Range(0, GridSize * GridSize).ToArray();
        new Random(seed * 31).Shuffle(order);
        return order;
    }

    private static void AssertSameBlockLight(Chunk expected, Chunk actual)
    {
        foreach (LightChannel channel in LightUtils.BlockVisibileColorChannels)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int y = FloorY - 1; y < FloorY + 9; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        var position = new Vector3i(x, y, z);
                        Assert.True(
                            LightUtils.GetLightOfChannel(expected, position, channel) ==
                            LightUtils.GetLightOfChannel(actual, position, channel),
                            $"{channel} light differs in chunk ({expected.GridX}, {expected.GridZ}) at {position}");
                    }
                }
            }
        }
    }

    private sealed class LightingWorld() : World(null!);
}
