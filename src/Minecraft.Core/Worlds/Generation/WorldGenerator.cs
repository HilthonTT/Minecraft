using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Storage;
using Minecraft.Core.Worlds.Structures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Generates chunk terrain off the main thread. Requests are served first come first served, and everyone
/// waiting on the same chunk position is answered from a single generation pass.
/// </summary>
public sealed class WorldGenerator
{
    /// <summary>Layers of <see cref="Biome.GradientBlock"/> placed between the surface and the stone below.</summary>
    private const int GradientDepth = 3;

    /// <summary>
    /// The layers of unbreakable floor at the bottom of the world. The lowest is solid; the ones above it are
    /// scattered, so the floor has a rough underside rather than reading as a flat slab.
    /// </summary>
    private const int BedrockDepth = 4;

    /// <summary>
    /// How far the ground has to fall away from a column, over a single block, before nothing will settle on
    /// it. Anything steeper is left as the bare rock of its biome.
    /// </summary>
    private const int CliffSlope = 3;

    /// <summary>
    /// How far above the waterline the ground is still washed bare. One block, so a beach is the strip the
    /// water actually reaches rather than a band of sand running inland.
    /// </summary>
    private const int BeachHeight = 1;

    /// <summary>
    /// How far below the waterline the floor stops being beach sand and becomes the gravel of the deep.
    /// </summary>
    private const int SeabedGravelDepth = 8;

    /// <summary>
    /// The height above which the ground is left under snow. Set so that snow caps the peaks and the highest
    /// shoulders below them rather than blanketing every hill that happens to be above average.
    /// </summary>
    private const int SnowLineY = 116;

    /// <summary>
    /// How far the snow line wanders up and down, and how quickly. Without it the snow would end at exactly
    /// the same height on every mountain and leave a contour line drawn around the range.
    /// </summary>
    private const float SnowLineJitter = 7F;
    private const float SnowLineDetail = 0.035F;

    /// <summary>
    /// How much of the ground above the snow line has been pressed into ice rather than left as snow, and
    /// over what distance it changes. Broad and gentle, so a summit carries a sheet of it rather than a
    /// speckle.
    /// </summary>
    private const float GlacierThreshold = 0.35F;
    private const float GlacierDetail = 0.012F;

    /// <summary>Kept away from the snow line's own field, which would otherwise draw ice along its edges.</summary>
    private const float GlacierDomainOffset = 7717.3F;

    /// <summary>Mixed into the seed so the bedrock floor is not the same pattern as anything else.</summary>
    private const uint BedrockSalt = 0x4245_4452u;

    /// <summary>
    /// The columns of the chunk plus a one block skirt around it. The skirt is never built, only measured:
    /// it is what lets a column on the very edge of a chunk see how far the ground drops away outside it, so
    /// a cliff face is recognised as one from both of the chunks it falls between.
    /// </summary>
    private const int SampledColumnDim = 18;

    private readonly WorldServer _world;
    private readonly WorldStorage _storage;
    private readonly int _seed;
    private readonly Biome[] _registeredBiomes;
    private readonly TerrainSampler _terrainSampler;
    private readonly CaveCarver _caveCarver = new();
    private readonly RavineCarver _ravineCarver = new();
    private readonly WaterfallGenerator _waterfallGenerator = new();
    private readonly DepositGenerator _depositGenerator;
    private readonly StructureGenerator _structureGenerator;

    private readonly Lock _generationLock = new();
    private readonly Dictionary<(World World, Vector2 GridPosition), List<GenerateChunkRequest>> _pendingRequests = [];
    private readonly Queue<GenerateChunkRequest> _generationOrder = new();
    private readonly Thread _terrainGeneratorThread;

    public int SeaLevel { get; } = 62;

    /// <summary>
    /// Where the ground is at any column, without the chunk holding it having to exist. Used to look ahead
    /// for somewhere to put a player before any of the terrain around them has been built.
    /// </summary>
    public ITerrainSampler TerrainSampler => _terrainSampler;

    public WorldGenerator(WorldServer world, WorldStorage storage, int seed)
    {
        _world = world;
        _storage = storage;
        _seed = seed;

        // The noise fields are shared static state, so seeding them here fixes the terrain for every biome.
        Noise2DPerlin.Reseed(seed);
        Noise3DPerlin.Reseed(seed);

        _registeredBiomes =
        [
            new PlainsBiome(),
            new ForestBiome(),
            new TaigaBiome(),
            new SwampBiome(),
            new SavannaBiome(),
            new DesertBiome(),
            new BadlandsBiome(),
            new MountainBiome(),
            new SnowyPeaksBiome(),
            new SnowyPlainsBiome(),
        ];

        _terrainSampler = new TerrainSampler(_registeredBiomes, SeaLevel, BedrockDepth + GradientDepth + 1);
        _depositGenerator = new DepositGenerator(seed);
        _structureGenerator = new StructureGenerator(seed);

        _terrainGeneratorThread = new Thread(RunChunkGeneration)
        {
            IsBackground = true,
            Name = "Chunk generation",
        };
        _terrainGeneratorThread.Start();
    }

    public void AddChunkGenerationRequest(GenerateChunkRequest request)
    {
        (World World, Vector2 GridPosition) key = (request.World, request.GridPosition);

        lock (_generationLock)
        {
            // Several players can want the same chunk. They all get queued, but it is only generated once.
            if (_pendingRequests.TryGetValue(key, out List<GenerateChunkRequest>? requests))
            {
                requests.Add(request);
            }
            else
            {
                _pendingRequests.Add(key, [request]);
            }

            _generationOrder.Enqueue(request);
        }
    }

    private void RunChunkGeneration()
    {
        while (true)
        {
            Thread.Sleep(5);

            GenerateChunkRequest request;
            List<GenerateChunkRequest> waitingRequests;

            lock (_generationLock)
            {
                if (_generationOrder.Count == 0)
                {
                    continue;
                }

                request = _generationOrder.Dequeue();

                (World World, Vector2 GridPosition) key = (request.World, request.GridPosition);
                if (!_pendingRequests.Remove(key, out List<GenerateChunkRequest>? requests))
                {
                    // An earlier queue entry for this position already answered this request along with its own.
                    continue;
                }

                waitingRequests = requests;
            }

            Chunk chunk = ProvideChunkAt((int)request.GridPosition.X, (int)request.GridPosition.Y);
            var output = new GenerateChunkOutput
            {
                Chunk = chunk,
                World = request.World,
            };

            foreach (GenerateChunkRequest waitingRequest in waitingRequests)
            {
                waitingRequest.Callback.Invoke(output);
            }
        }
    }

    /// <summary>
    /// The chunk at the given position, loaded from disk if it was ever modified and generated from the
    /// seed otherwise. This is the only way a chunk should be brought into the world.
    /// </summary>
    public Chunk ProvideChunkAt(int chunkX, int chunkZ)
    {
        return _storage.TryLoadChunk(_world, chunkX, chunkZ) ?? GenerateBlocksForChunkAt(chunkX, chunkZ);
    }

    /// <summary>
    /// Mixes the world seed with a chunk position into a seed for that chunk's decoration.
    /// <para>
    /// Deliberately not <see cref="HashCode.Combine{T1, T2, T3}"/>: that is randomised per process, so a
    /// world would decorate differently every time it was loaded and stored chunks would stop lining up
    /// with regenerated neighbours.
    /// </para>
    /// </summary>
    private static int GetChunkSeed(int seed, int chunkX, int chunkZ)
    {
        unchecked
        {
            // Multiply and xor-shift so that neighbouring chunks get unrelated seeds rather than adjacent
            // ones, which a plain sum would produce.
            uint hash = (uint)seed;
            hash = (hash ^ (uint)chunkX) * 2654435761u;
            hash = (hash ^ (uint)chunkZ) * 2246822519u;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)hash;
        }
    }

    public Chunk GenerateBlocksForChunkAt(int chunkX, int chunkZ)
    {
        Chunk chunk = _world.ChunkPool.GetObject();
        chunk.ResetAndAssign(chunkX, chunkZ);

        const int chunkDim = 16;

        // Derived from the seed and the position so decoration is reproducible, which is what lets an
        // unmodified chunk be regenerated instead of stored.
        var random = new Random(GetChunkSeed(_seed, chunkX, chunkZ));

        TerrainColumn[,] sampledColumns = SampleColumnsWithSkirt(chunkX, chunkZ);

        // Kept for the passes after this one: caves need to know how deep each column is buried, and
        // decoration needs to know what it is standing on.
        var surfaceHeights = new int[chunkDim, chunkDim];
        var surfaceBiomes = new Biome[chunkDim, chunkDim];

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                BuildColumn(chunk, sampledColumns, chunkX, chunkZ, localX, localZ, surfaceHeights, surfaceBiomes, SeaLevel);
            }
        }

        LayBedrockFloor(chunk);

        // Before the caves, so that a tunnel cutting through a vein leaves its face showing in the wall.
        _depositGenerator.PlaceDepositsIn(chunk);

        // Carved before anything is decorated, so that a tunnel breaking through the surface does not leave
        // a tree or a flower hanging over the hole it opened.
        _caveCarver.Carve(chunk, surfaceHeights);

        // After the caves, so that a gorge cutting down through one opens its side into the ravine rather
        // than the tunnel being carved back out of the wall the gorge left.
        _ravineCarver.Carve(chunk, surfaceHeights);

        // After both, so that a tunnel opening into the side of a lake does not fill up through it.
        FillWaterUpToSeaLevel(chunk, surfaceHeights);

        // After the water, so a spring reads what is standing in the cliff below it, and before the
        // decoration, which leaves alone anything a fall has already landed in.
        _waterfallGenerator.PlaceWaterfallsIn(chunk, surfaceHeights, surfaceBiomes, SeaLevel, random);

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int surfaceY = surfaceHeights[localX, localZ];

                // A cave mouth took the surface with it, so there is nothing left here to decorate.
                if (chunk.GetBlockAt(localX, surfaceY, localZ).GetBlock() == BlockRegistry.Air)
                {
                    continue;
                }

                // Nothing grows on a seabed. Read off the block rather than compared against sea level, so
                // that whatever decides where the water goes only has to be right in one place.
                if (chunk.GetBlockAt(localX, surfaceY + 1, localZ).GetBlock() == BlockRegistry.Water)
                {
                    continue;
                }

                // Decoration sits on top of the surface block.
                surfaceBiomes[localX, localZ].Decorator.Decorate(chunk, surfaceY + 1, localX, localZ, random);
            }
        }

        // Last, so that a building clears away the trees and undergrowth standing where it goes up rather
        // than being decorated over afterwards.
        _structureGenerator.PlaceStructuresIn(chunk, _terrainSampler);

        // Freshly generated terrain matches what the generator would produce by definition, so there is
        // nothing here worth writing to disk yet.
        chunk.MarkClean();
        return chunk;
    }

    /// <summary>
    /// Samples the chunk's own columns along with a one block skirt around it, indexed so that the chunk's
    /// column (0, 0) sits at (1, 1).
    /// </summary>
    private TerrainColumn[,] SampleColumnsWithSkirt(int chunkX, int chunkZ)
    {
        var columns = new TerrainColumn[SampledColumnDim, SampledColumnDim];

        for (int x = 0; x < SampledColumnDim; x++)
        {
            for (int z = 0; z < SampledColumnDim; z++)
            {
                columns[x, z] = _terrainSampler.SampleColumn(
                    (chunkX * 16) + x - 1,
                    (chunkZ * 16) + z - 1);
            }
        }

        return columns;
    }

    /// <summary>
    /// Fills one column from the bottom of the world up to its surface, and records what the passes after
    /// this one need to know about it.
    /// </summary>
    private static void BuildColumn(
        Chunk chunk,
        TerrainColumn[,] sampledColumns,
        int chunkX,
        int chunkZ,
        int localX,
        int localZ,
        int[,] surfaceHeights,
        Biome[,] surfaceBiomes,
        int seaLevel)
    {
        (int surfaceY, Biome biome) = sampledColumns[localX + 1, localZ + 1];

        surfaceHeights[localX, localZ] = surfaceY;
        surfaceBiomes[localX, localZ] = biome;

        int slope = GetSteepestDrop(sampledColumns, localX + 1, localZ + 1, surfaceY);

        (Block top, Block gradient) = GetSurfaceBlocks(
            biome,
            surfaceY,
            slope,
            (chunkX * 16) + localX,
            (chunkZ * 16) + localZ,
            seaLevel);

        chunk.AddBlockAt(localX, surfaceY, localZ, BlockRegistry.GetState(top));

        for (int y = surfaceY - 1; y >= surfaceY - GradientDepth; y--)
        {
            chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(gradient));
        }

        for (int y = surfaceY - GradientDepth - 1; y >= 0; y--)
        {
            chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(BlockRegistry.Stone));
        }
    }

    /// <summary>
    /// How far the ground falls away from a column to the lowest of its four neighbours. Only the drop is
    /// measured and not the rise, since it is the face below a column that is exposed and has to be bare.
    /// </summary>
    private static int GetSteepestDrop(TerrainColumn[,] sampledColumns, int x, int z, int surfaceY)
    {
        int lowestNeighbour = Math.Min(
            Math.Min(sampledColumns[x - 1, z].SurfaceY, sampledColumns[x + 1, z].SurfaceY),
            Math.Min(sampledColumns[x, z - 1].SurfaceY, sampledColumns[x, z + 1].SurfaceY));

        return surfaceY - lowestNeighbour;
    }

    /// <summary>
    /// What a column wears on top and immediately underneath, which is its biome's own soil except where the
    /// ground is too high or too steep to hold any.
    /// </summary>
    private static (Block Top, Block Gradient) GetSurfaceBlocks(
        Biome biome,
        int surfaceY,
        int slope,
        int worldX,
        int worldZ,
        int seaLevel)
    {
        // Nothing settles on a cliff face, so what shows is the rock the biome is cut into. Tested before the
        // snow, because snow does not lie on a wall either.
        if (slope >= CliffSlope)
        {
            Block cliff = biome.CliffAt(surfaceY);
            return (cliff, cliff);
        }

        // Anything the water reaches is washed down to bare sand, whichever biome it nominally belongs to,
        // which is what puts a beach around every sea and lake instead of grass running into the water. The
        // deep floor further out is gravel, so a sea reads as getting deeper rather than as one flat basin.
        // The wetlands are the exception: their water is the biome rather than a sea they happen to meet.
        if (surfaceY <= seaLevel + BeachHeight && biome.HasShoreline)
        {
            return surfaceY < seaLevel - SeabedGravelDepth
                ? (BlockRegistry.Gravel, BlockRegistry.Gravel)
                : (BlockRegistry.Sand, BlockRegistry.Sand);
        }

        float jitter = Noise2DPerlin.Noise(worldX * SnowLineDetail, worldZ * SnowLineDetail) * SnowLineJitter;
        if (surfaceY + jitter >= SnowLineY)
        {
            // Where the snow has lain long enough it has become a sheet of ice, which is what puts a glacier
            // across part of a summit instead of the whole thing being the same white.
            float glacier = Noise2DPerlin.Noise(
                (worldX * GlacierDetail) + GlacierDomainOffset,
                (worldZ * GlacierDetail) + GlacierDomainOffset);

            Block cover = glacier > GlacierThreshold ? BlockRegistry.Ice : BlockRegistry.Snow;
            return (cover, BlockRegistry.Stone);
        }

        return biome.SurfaceAt(surfaceY);
    }

    /// <summary>
    /// Fills everything standing open below the waterline with water, which is what puts the sea into an
    /// ocean basin, the water into a river channel and a lake into any hollow that happens to fall below it.
    /// <para>
    /// Only the space above each column's own surface is filled. A cave that runs below the seabed is left
    /// as the air it was carved into rather than flooded up through the rock, so breaking into one from
    /// underneath finds a dry tunnel instead of the whole ocean.
    /// </para>
    /// </summary>
    private void FillWaterUpToSeaLevel(Chunk chunk, int[,] surfaceHeights)
    {
        BlockState water = BlockRegistry.GetState(BlockRegistry.Water);

        for (int localX = 0; localX < 16; localX++)
        {
            for (int localZ = 0; localZ < 16; localZ++)
            {
                for (int y = surfaceHeights[localX, localZ] + 1; y <= SeaLevel; y++)
                {
                    if (chunk.GetBlockAt(localX, y, localZ).GetBlock() == BlockRegistry.Air)
                    {
                        chunk.AddBlockAt(localX, y, localZ, water);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Lays the floor of the world. The bottom layer is solid and the ones above it thin out with height, so
    /// what a player standing in a deep cave sees underfoot is a ragged crust rather than a flat plate.
    /// </summary>
    private void LayBedrockFloor(Chunk chunk)
    {
        BlockState bedrock = BlockRegistry.GetState(BlockRegistry.Bedrock);

        for (int localX = 0; localX < 16; localX++)
        {
            for (int localZ = 0; localZ < 16; localZ++)
            {
                chunk.AddBlockAt(localX, 0, localZ, bedrock);

                for (int y = 1; y < BedrockDepth; y++)
                {
                    // Thins from nearly solid just above the floor to nearly nothing at the top layer.
                    uint threshold = (uint)((BedrockDepth - y) * (uint.MaxValue / BedrockDepth));

                    if (GetBedrockNoiseAt(chunk.GridX * 16 + localX, y, chunk.GridZ * 16 + localZ) < threshold)
                    {
                        chunk.AddBlockAt(localX, y, localZ, bedrock);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A value spread evenly over the whole range of a <see cref="uint"/> for a world position, so that the
    /// floor is scattered the same way every time the chunk is generated.
    /// </summary>
    private uint GetBedrockNoiseAt(int worldX, int y, int worldZ)
    {
        unchecked
        {
            uint hash = (uint)_seed ^ BedrockSalt;
            hash = (hash ^ (uint)worldX) * 2654435761u;
            hash = (hash ^ (uint)y) * 2246822519u;
            hash = (hash ^ (uint)worldZ) * 3266489917u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return hash;
        }
    }
}
