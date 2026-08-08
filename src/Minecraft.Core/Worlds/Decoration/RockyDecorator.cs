using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Mountainside: mostly bare rock, patched with gravel and moss, with pockets of soil in the sheltered parts
/// carrying what little grows this high up.
/// </summary>
public sealed class RockyDecorator : IDecorator
{
    /// <summary>How quickly the ground changes from one kind of cover to the next.</summary>
    private const float TerrainDetail = 0.0075F;

    /// <summary>The same, for the scatter of plants over the soil, which varies far more tightly.</summary>
    private const float FoliageDetail = 0.75F;

    private readonly PineTreeGenerator _pineTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        // Snow lies over the summits, and nothing takes root through it.
        if (SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ) == BlockRegistry.Snow)
        {
            return;
        }

        int worldX = chunk.GridX * 16 + localX;
        int worldZ = chunk.GridZ * 16 + localZ;

        // Large scale noise decides which patches of the mountain are bare rock, gravel, moss or soil.
        float terrainType = Noise3DPerlin.Noise(worldX * TerrainDetail, worldY * TerrainDetail, worldZ * TerrainDetail);

        if (terrainType < -0.75F)
        {
            chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.Gravel));
            return;
        }

        if (terrainType > 0.8F)
        {
            // The damp side of the mountain, where the rock has gone over to moss.
            chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.MossyCobblestone));

            if (random.Next(150) == 1)
            {
                Block mushroom = random.Next(2) == 0 ? BlockRegistry.RedMushroom : BlockRegistry.BrownMushroom;
                chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(mushroom));
            }

            return;
        }

        if (terrainType >= -0.45F)
        {
            if (random.Next(700) == 1)
            {
                SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.Cobblestone, worldY, localX, localZ, random);
            }

            return;
        }

        for (int depth = 2; depth <= 3; depth++)
        {
            chunk.AddBlockAt(localX, worldY - depth, localZ, BlockRegistry.GetState(BlockRegistry.Dirt));
        }

        // Grass rather than bare dirt on top, so a soil patch reads as a meadow caught between the crags.
        chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.Grass));

        // A second, much higher frequency sample scatters foliage across the soil patches.
        float foliage = Noise3DPerlin.Noise(worldX * FoliageDetail, worldY * FoliageDetail, worldZ * FoliageDetail);
        if (foliage < -0.9F)
        {
            _pineTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
        else if (foliage < -0.5F)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (foliage > 0.94F)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.Dandelion));
        }
    }
}
