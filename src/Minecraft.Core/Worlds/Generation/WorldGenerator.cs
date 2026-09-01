using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Storage;
using Minecraft.Core.Worlds.Structures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

public sealed class WorldGenerator
{
    private const int GradientDepth = 3;

    private const int BedrockDepth = 4;

    private const int CliffSlope = 3;

    private const int BeachHeight = 1;

    private const int SeabedGravelDepth = 8;

    private const int SnowLineY = 116;

    private const float SnowLineJitter = 7F;
    private const float SnowLineDetail = 0.035F;

    private const float GlacierThreshold = 0.35F;
    private const float GlacierDetail = 0.012F;

    private const float GlacierDomainOffset = 7717.3F;

    private const uint BedrockSalt = 0x4245_4452u;

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

    public ITerrainSampler TerrainSampler => _terrainSampler;

    public WorldGenerator(WorldServer world, WorldStorage storage, int seed)
    {
        _world = world;
        _storage = storage;
        _seed = seed;

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

    public Chunk ProvideChunkAt(int chunkX, int chunkZ)
    {
        return _storage.TryLoadChunk(_world, chunkX, chunkZ) ?? GenerateBlocksForChunkAt(chunkX, chunkZ);
    }

    private static int GetChunkSeed(int seed, int chunkX, int chunkZ)
    {
        unchecked
        {
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

        var random = new Random(GetChunkSeed(_seed, chunkX, chunkZ));

        TerrainColumn[,] sampledColumns = SampleColumnsWithSkirt(chunkX, chunkZ);

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

        _depositGenerator.PlaceDepositsIn(chunk);

        _caveCarver.Carve(chunk, surfaceHeights);

        _ravineCarver.Carve(chunk, surfaceHeights);

        FillWaterUpToSeaLevel(chunk, surfaceHeights);

        _waterfallGenerator.PlaceWaterfallsIn(chunk, surfaceHeights, surfaceBiomes, SeaLevel, random);

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int surfaceY = surfaceHeights[localX, localZ];

                if (chunk.GetBlockAt(localX, surfaceY, localZ).GetBlock() == BlockRegistry.Air)
                {
                    continue;
                }

                if (chunk.GetBlockAt(localX, surfaceY + 1, localZ).GetBlock() == BlockRegistry.Water)
                {
                    continue;
                }

                surfaceBiomes[localX, localZ].Decorator.Decorate(chunk, surfaceY + 1, localX, localZ, random);
            }
        }

        _structureGenerator.PlaceStructuresIn(chunk, _terrainSampler);

        chunk.MarkClean();
        return chunk;
    }

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

    private static int GetSteepestDrop(TerrainColumn[,] sampledColumns, int x, int z, int surfaceY)
    {
        int lowestNeighbour = Math.Min(
            Math.Min(sampledColumns[x - 1, z].SurfaceY, sampledColumns[x + 1, z].SurfaceY),
            Math.Min(sampledColumns[x, z - 1].SurfaceY, sampledColumns[x, z + 1].SurfaceY));

        return surfaceY - lowestNeighbour;
    }

    private static (Block Top, Block Gradient) GetSurfaceBlocks(
        Biome biome,
        int surfaceY,
        int slope,
        int worldX,
        int worldZ,
        int seaLevel)
    {
        if (slope >= CliffSlope)
        {
            Block cliff = biome.CliffAt(surfaceY);
            return (cliff, cliff);
        }

        if (surfaceY <= seaLevel + BeachHeight && biome.HasShoreline)
        {
            return surfaceY < seaLevel - SeabedGravelDepth
                ? (BlockRegistry.Gravel, BlockRegistry.Gravel)
                : (BlockRegistry.Sand, BlockRegistry.Sand);
        }

        float jitter = Noise2DPerlin.Noise(worldX * SnowLineDetail, worldZ * SnowLineDetail) * SnowLineJitter;
        if (surfaceY + jitter >= SnowLineY)
        {
            float glacier = Noise2DPerlin.Noise(
                (worldX * GlacierDetail) + GlacierDomainOffset,
                (worldZ * GlacierDetail) + GlacierDomainOffset);

            Block cover = glacier > GlacierThreshold ? BlockRegistry.Ice : BlockRegistry.Snow;
            return (cover, BlockRegistry.Stone);
        }

        return biome.SurfaceAt(surfaceY);
    }

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
                    uint threshold = (uint)((BedrockDepth - y) * (uint.MaxValue / BedrockDepth));

                    if (GetBedrockNoiseAt(chunk.GridX * 16 + localX, y, chunk.GridZ * 16 + localZ) < threshold)
                    {
                        chunk.AddBlockAt(localX, y, localZ, bedrock);
                    }
                }
            }
        }
    }

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
