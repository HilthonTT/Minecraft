using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

public static class SurfaceFeatures
{
    public static void PlaceBoulder(Chunk chunk, Block stone, int worldY, int localX, int localZ, Random random)
    {
        int radius = 1 + random.Next(2);

        if (localX < radius || localX >= 16 - radius || localZ < radius || localZ >= 16 - radius)
        {
            return;
        }

        if (worldY + radius >= Constants.MAX_BUILD_HEIGHT)
        {
            return;
        }

        BlockState state = BlockRegistry.GetState(stone);

        int baseY = worldY - 1;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = 0; dy <= radius; dy++)
                {
                    if ((dx * dx) + (dy * dy) + (dz * dz) > radius * radius)
                    {
                        continue;
                    }

                    chunk.AddBlockAt(localX + dx, baseY + dy, localZ + dz, state);
                }
            }
        }
    }

    public static Block GetGroundAt(Chunk chunk, int worldY, int localX, int localZ)
    {
        return chunk.GetBlockAt(localX, worldY - 1, localZ).GetBlock();
    }
}
