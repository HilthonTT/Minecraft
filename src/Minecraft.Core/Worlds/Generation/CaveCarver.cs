using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

public sealed class CaveCarver
{
    private const int LowestCaveY = 3;

    private const float SurfaceFadeDepth = 6F;

    private const float CeilingFadeDepth = 16F;

    private const float PairDomainOffset = 4271.3F;

    private const float EntranceDetail = 0.0045F;
    private const float EntranceDomainOffset = 2903.7F;

    private const float EntranceThreshold = 0.62F;
    private const float EntranceFullyOpen = 0.86F;

    private readonly record struct TunnelLayer(
        float HorizontalDetail,
        float VerticalDetail,
        float Thickness,
        float DomainOffset,
        int CeilingY);

    private readonly TunnelLayer[] _layers =
    [
        new TunnelLayer(
            HorizontalDetail: 0.020F,
            VerticalDetail: 0.030F,
            Thickness: 0.075F,
            DomainOffset: 0F,
            CeilingY: Constants.MAX_BUILD_HEIGHT),

        new TunnelLayer(
            HorizontalDetail: 0.008F,
            VerticalDetail: 0.013F,
            Thickness: 0.050F,
            DomainOffset: 1783.9F,
            CeilingY: 48),
    ];

    public void Carve(Chunk chunk, int[,] surfaceHeights)
    {
        const int chunkDim = 16;

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int worldX = chunk.GridX * chunkDim + localX;
                int worldZ = chunk.GridZ * chunkDim + localZ;
                int surfaceY = surfaceHeights[localX, localZ];
                float entrance = EntranceOpennessAt(worldX, worldZ);

                for (int y = LowestCaveY; y <= surfaceY; y++)
                {
                    if (!IsHollowAt(worldX, y, worldZ, surfaceY, entrance))
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

    private static float EntranceOpennessAt(int worldX, int worldZ)
    {
        float mask = Noise2DPerlin.Noise01(
            worldX * EntranceDetail + EntranceDomainOffset,
            worldZ * EntranceDetail + EntranceDomainOffset);

        return Math.Clamp((mask - EntranceThreshold) / (EntranceFullyOpen - EntranceThreshold), 0F, 1F);
    }

    private bool IsHollowAt(int worldX, int worldY, int worldZ, int surfaceY, float entranceOpenness)
    {
        float depthFade = Math.Clamp((surfaceY + 1 - worldY) / SurfaceFadeDepth, 0F, 1F);

        float surfaceFade = depthFade + ((1F - depthFade) * entranceOpenness);

        foreach (TunnelLayer layer in _layers)
        {
            float ceilingFade = Math.Clamp((layer.CeilingY - worldY) / CeilingFadeDepth, 0F, 1F);
            if (ceilingFade <= 0F)
            {
                continue;
            }

            float thickness = layer.Thickness * surfaceFade * ceilingFade;

            float x = worldX * layer.HorizontalDetail + layer.DomainOffset;
            float y = worldY * layer.VerticalDetail + layer.DomainOffset;
            float z = worldZ * layer.HorizontalDetail + layer.DomainOffset;

            float first = Noise3DPerlin.Noise(x, y, z);
            if (MathF.Abs(first) >= thickness)
            {
                continue;
            }

            float second = Noise3DPerlin.Noise(
                x + PairDomainOffset,
                y + PairDomainOffset,
                z + PairDomainOffset);

            if ((first * first) + (second * second) < thickness * thickness)
            {
                return true;
            }
        }

        return false;
    }
}
