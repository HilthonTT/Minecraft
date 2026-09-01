using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public abstract class TreeGenerator : ITreeGenerator
{
    protected abstract int CanopyRadius { get; }

    protected abstract int MaxHeight { get; }

    public void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ, Random random)
    {
        if (localX < CanopyRadius || localX >= 16 - CanopyRadius ||
            localZ < CanopyRadius || localZ >= 16 - CanopyRadius)
        {
            return;
        }

        if (worldY + MaxHeight >= Constants.MAX_BUILD_HEIGHT)
        {
            return;
        }

        Grow(chunk, localX, worldY, localZ, random);
    }

    protected abstract void Grow(Chunk chunk, int localX, int worldY, int localZ, Random random);

    protected static void PlaceTrunk(Chunk chunk, BlockState log, int localX, int worldY, int localZ, int height)
    {
        for (int y = 0; y < height; y++)
        {
            chunk.AddBlockAt(localX, worldY + y, localZ, log);
        }
    }

    protected static void PlaceLeafDisc(
        Chunk chunk,
        BlockState leaves,
        int centreX,
        int worldY,
        int centreZ,
        int radius,
        bool cutCorners)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                if (cutCorners && Math.Abs(dx) == radius && Math.Abs(dz) == radius)
                {
                    continue;
                }

                if (chunk.GetBlockAt(centreX + dx, worldY, centreZ + dz).GetBlock() != BlockRegistry.Air)
                {
                    continue;
                }

                chunk.AddBlockAt(centreX + dx, worldY, centreZ + dz, leaves);
            }
        }
    }
}
