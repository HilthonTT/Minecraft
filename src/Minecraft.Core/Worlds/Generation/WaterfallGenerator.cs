using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

public sealed class WaterfallGenerator
{
    private const int MinDrop = 6;

    private const int MaxDrop = 64;

    private const double MinMoisture = 0.40D;

    private const int SpringOdds = 3;
    private const int MaxSpringsPerChunk = 2;

    private static readonly (int X, int Z)[] _neighbourOffsets = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    public void PlaceWaterfallsIn(
        Chunk chunk,
        int[,] surfaceHeights,
        Biome[,] surfaceBiomes,
        int seaLevel,
        Random random)
    {
        int placed = 0;

        for (int localX = 1; localX < 15 && placed < MaxSpringsPerChunk; localX++)
        {
            for (int localZ = 1; localZ < 15 && placed < MaxSpringsPerChunk; localZ++)
            {
                if (surfaceBiomes[localX, localZ].Moisture < MinMoisture)
                {
                    continue;
                }

                if (FindFall(chunk, surfaceHeights, localX, localZ, seaLevel) is not (int faceX, int faceZ, int footY))
                {
                    continue;
                }

                if (random.Next(SpringOdds) != 0)
                {
                    continue;
                }

                WriteFall(chunk, faceX, faceZ, footY, surfaceHeights[localX, localZ]);
                placed++;
            }
        }
    }

    private static (int X, int Z, int FootY)? FindFall(
        Chunk chunk,
        int[,] surfaceHeights,
        int localX,
        int localZ,
        int seaLevel)
    {
        int lipY = surfaceHeights[localX, localZ];

        if (lipY <= seaLevel + 1 || IsAir(chunk, localX, lipY, localZ))
        {
            return null;
        }

        foreach ((int offsetX, int offsetZ) in _neighbourOffsets)
        {
            int faceX = localX + offsetX;
            int faceZ = localZ + offsetZ;

            if (!IsAir(chunk, faceX, lipY, faceZ))
            {
                continue;
            }

            int footY = FindFloorBelow(chunk, faceX, faceZ, lipY);
            if (footY < 0 || lipY - footY < MinDrop)
            {
                continue;
            }

            return (faceX, faceZ, footY);
        }

        return null;
    }

    private static int FindFloorBelow(Chunk chunk, int faceX, int faceZ, int lipY)
    {
        int lowest = Math.Max(0, lipY - MaxDrop);

        for (int y = lipY - 1; y >= lowest; y--)
        {
            Block block = chunk.GetBlockAt(faceX, y, faceZ).GetBlock();

            if (block == BlockRegistry.Air)
            {
                continue;
            }

            return block.IsLiquid ? -1 : y;
        }

        return -1;
    }

    private static bool IsAir(Chunk chunk, int localX, int worldY, int localZ)
    {
        return chunk.GetBlockAt(localX, worldY, localZ).GetBlock() == BlockRegistry.Air;
    }

    private static void WriteFall(Chunk chunk, int faceX, int faceZ, int footY, int lipY)
    {
        BlockState source = BlockRegistry.GetState(BlockRegistry.Water);
        BlockState falling = BlockRegistry.GetState(BlockRegistry.WaterFalling);

        chunk.AddBlockAt(faceX, lipY, faceZ, source);

        for (int y = footY + 2; y < lipY; y++)
        {
            chunk.AddBlockAt(faceX, y, faceZ, falling);
        }

        chunk.AddBlockAt(faceX, footY + 1, faceZ, source);
    }
}
