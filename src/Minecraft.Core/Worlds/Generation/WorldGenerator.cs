using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Generates chunk terrain off the main thread. Requests are served first come first served, and everyone
/// waiting on the same chunk position is answered from a single generation pass.
/// </summary>
public sealed class WorldGenerator
{
    /// <summary>How quickly the climate varies across the world. Smaller means larger biomes.</summary>
    private const double TemperatureDetail = 0.0075D;
    private const double MoistureDetail = 0.0075D;

    /// <summary>
    /// Temperature and moisture come from the same shared noise field, so moisture is sampled from a distant
    /// part of the domain to keep the two climate axes independent.
    /// </summary>
    private const float MoistureDomainOffset = 2555.5F;

    /// <summary>Layers of <see cref="Biome.GradientBlock"/> placed between the surface and the stone below.</summary>
    private const int GradientDepth = 3;

    private readonly WorldServer _world;
    private readonly Biome[] _registeredBiomes;
    private readonly BiomeProvider _biomeProvider;

    private readonly Lock _generationLock = new();
    private readonly Dictionary<(World World, Vector2 GridPosition), List<GenerateChunkRequest>> _pendingRequests = [];
    private readonly Queue<GenerateChunkRequest> _generationOrder = new();
    private readonly Thread _terrainGeneratorThread;

    public int SeaLevel { get; } = 62;

    public WorldGenerator(WorldServer world)
    {
        _world = world;

        _registeredBiomes =
        [
            new MountainBiome(),
            new DesertBiome(),
            new ForestBiome(),
        ];

        _biomeProvider = new BiomeProvider(_registeredBiomes);

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

            Chunk chunk = GenerateBlocksForChunkAt((int)request.GridPosition.X, (int)request.GridPosition.Y);
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

    public Chunk GenerateBlocksForChunkAt(int chunkX, int chunkZ)
    {
        Chunk chunk = _world.ChunkPool.GetObject();
        chunk.ResetAndAssign(chunkX, chunkZ);

        const int chunkDim = 16;

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int worldX = chunkX * chunkDim + localX;
                int worldZ = chunkZ * chunkDim + localZ;

                double temperature = Noise2DPerlin.Noise01(
                    (float)(worldZ * TemperatureDetail),
                    (float)(worldX * TemperatureDetail));
                double moisture = Noise2DPerlin.Noise01(
                    (float)(worldZ * MoistureDetail) + MoistureDomainOffset,
                    (float)(worldX * MoistureDetail) + MoistureDomainOffset);

                BiomeMembership[] memberships = _biomeProvider.GetBiomeMemberships(temperature, moisture);

                // The surface blocks come from whichever biome dominates, but the height is a blend of all
                // of them so that the transition between two biomes is a slope rather than a cliff.
                BiomeMembership dominant = memberships[0];
                double heightOffset = 0;
                foreach (BiomeMembership membership in memberships)
                {
                    if (dominant.Percentage < membership.Percentage)
                    {
                        dominant = membership;
                    }

                    heightOffset += membership.Percentage * membership.Biome.OffsetAt(chunkX, chunkZ, localX, localZ);
                }

                // Clamped so that neither the gradient layers below nor the decoration above can ever fall
                // outside the build height.
                int surfaceY = Math.Clamp(
                    SeaLevel + (int)heightOffset,
                    GradientDepth + 1,
                    Constants.MAX_BUILD_HEIGHT - 2);

                chunk.AddBlockAt(localX, surfaceY, localZ, BlockRegistry.GetState(dominant.Biome.TopBlock));

                for (int y = surfaceY - 1; y >= surfaceY - GradientDepth; y--)
                {
                    chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(dominant.Biome.GradientBlock));
                }

                for (int y = surfaceY - GradientDepth - 1; y >= 0; y--)
                {
                    chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(BlockRegistry.Stone));
                }

                // Decoration sits on top of the surface block.
                dominant.Biome.Decorator.Decorate(chunk, surfaceY + 1, localX, localZ);
            }
        }

        return chunk;
    }
}
