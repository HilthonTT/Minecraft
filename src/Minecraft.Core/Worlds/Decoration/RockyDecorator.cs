using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Trees;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class RockyDecorator : IDecorator
{
    private readonly OakTreeGenerator _oakTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ)
    {
        int worldX = chunk.GridX * 16 + localX;
        int worldZ = chunk.GridZ * 16 + localZ;

        // Large scale noise decides which patches of the mountain are bare rock, gravel or soil.
        float terrainType = Noise3DPerlin.Noise(worldX * 0.0075F, worldY * 0.0075F, worldZ * 0.0075F);
        if (terrainType < -0.75F)
        {
            chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.Gravel));
            return;
        }

        if (terrainType >= -0.45F)
        {
            return;
        }

        for (int depth = 1; depth <= 3; depth++)
        {
            chunk.AddBlockAt(localX, worldY - depth, localZ, BlockRegistry.GetState(BlockRegistry.Dirt));
        }

        // A second, much higher frequency sample scatters foliage across the soil patches.
        float foliage = Noise3DPerlin.Noise(worldX * 0.75F, worldY * 0.75F, worldZ * 0.75F);
        if (foliage < -0.9F)
        {
            _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ);
        }
        else if (foliage < -0.5F)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
    }
}
