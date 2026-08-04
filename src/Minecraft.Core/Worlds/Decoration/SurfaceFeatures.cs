using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Small things a decorator drops on the surface that are more than a single block but far less than a
/// structure, and that several biomes want.
/// </summary>
public static class SurfaceFeatures
{
    /// <summary>
    /// Piles a rounded heap of stone on the ground. Like a tree it is refused rather than clipped where it
    /// would reach past the edge of the chunk, since the neighbour it would spill into is not loaded.
    /// </summary>
    /// <param name="worldY">The first free block above the ground.</param>
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

        // Sunk a block into the ground so the heap sits in the hillside rather than balancing on top of it.
        int baseY = worldY - 1;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = 0; dy <= radius; dy++)
                {
                    // Corners left off at every height, which rounds the heap instead of stacking a cube.
                    if ((dx * dx) + (dy * dy) + (dz * dz) > radius * radius)
                    {
                        continue;
                    }

                    chunk.AddBlockAt(localX + dx, baseY + dy, localZ + dz, state);
                }
            }
        }
    }

    /// <summary>The block a column is standing on, which is what decides whether anything will grow there.</summary>
    /// <param name="worldY">The first free block above the ground.</param>
    public static Block GetGroundAt(Chunk chunk, int worldY, int localX, int localZ)
    {
        return chunk.GetBlockAt(localX, worldY - 1, localZ).GetBlock();
    }
}
