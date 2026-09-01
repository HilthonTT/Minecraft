using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

public sealed class RavineCarver
{
    private const float ChannelDetail = 0.0016F;
    private const int ChannelOctaves = 2;
    private const float ChannelDomainOffset = 5717.61F;

    private const float ChannelWidth = 0.014F;

    private const float RegionDetail = 0.0022F;
    private const float RegionDomainOffset = 3391.07F;
    private const float RegionThreshold = 0.58F;
    private const float RegionFullyOpen = 0.66F;

    private const int MaxDepth = 52;
    private const int LowestFloorY = 14;

    private const float FloorDetail = 0.010F;
    private const float FloorDomainOffset = 8123.93F;
    private const int FloorVariation = 14;

    private const float FloorWidthShare = 0.28F;

    private const float FlareDepth = 26F;

    public void Carve(Chunk chunk, int[,] surfaceHeights)
    {
        const int chunkDim = 16;

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int worldX = chunk.GridX * chunkDim + localX;
                int worldZ = chunk.GridZ * chunkDim + localZ;

                float region = RegionOpennessAt(worldX, worldZ);
                if (region <= 0F)
                {
                    continue;
                }

                int surfaceY = surfaceHeights[localX, localZ];
                int floorY = GetFloorY(worldX, worldZ, surfaceY);
                if (floorY >= surfaceY)
                {
                    continue;
                }

                for (int y = floorY; y <= surfaceY; y++)
                {
                    if (!IsInsideRavine(worldX, y, worldZ, surfaceY, region))
                    {
                        continue;
                    }

                    if (chunk.GetBlockAt(localX, y, localZ).GetBlock() == BlockRegistry.Air)
                    {
                        continue;
                    }

                    chunk.RemoveBlockAt(localX, y, localZ);
                }
            }
        }
    }

    private static bool IsInsideRavine(int worldX, int worldY, int worldZ, int surfaceY, float region)
    {
        float depthShare = Math.Clamp((surfaceY - worldY) / FlareDepth, 0F, 1F);

        float width = ChannelWidth * region * (FloorWidthShare + ((1F - FloorWidthShare) * (1F - depthShare)));

        float channel = Noise2DPerlinOctave.Noise(
            worldZ * ChannelDetail + ChannelDomainOffset,
            worldX * ChannelDetail + ChannelDomainOffset,
            ChannelOctaves);

        return MathF.Abs(channel) < width;
    }

    private static int GetFloorY(int worldX, int worldZ, int surfaceY)
    {
        float wander = Noise2DPerlin.Noise01(
            worldX * FloorDetail + FloorDomainOffset,
            worldZ * FloorDetail + FloorDomainOffset);

        int depth = MaxDepth - (int)(wander * FloorVariation);
        return Math.Max(LowestFloorY, surfaceY - depth);
    }

    private static float RegionOpennessAt(int worldX, int worldZ)
    {
        float region = Noise2DPerlin.Noise01(
            worldX * RegionDetail + RegionDomainOffset,
            worldZ * RegionDetail + RegionDomainOffset);

        return Math.Clamp((region - RegionThreshold) / (RegionFullyOpen - RegionThreshold), 0F, 1F);
    }
}
