using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public sealed class OakTreeGenerator : ITreeGenerator
{
    /// <summary>
    /// The canopy reaches two blocks out from the trunk, so a trunk closer than this to a chunk edge would
    /// need to write into the neighbouring chunk, which is not loaded while this one is being generated.
    /// </summary>
    private const int CanopyMargin = 2;

    public void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ, Random random)
    {
        if (localX <= CanopyMargin || localX >= 16 - CanopyMargin ||
            localZ <= CanopyMargin || localZ >= 16 - CanopyMargin)
        {
            return;
        }

        BlockState leaves = BlockRegistry.GetState(BlockRegistry.OakLeaves);
        BlockState log = BlockRegistry.GetState(BlockRegistry.OakLog);

        int trunkX = localX;
        int trunkZ = localZ;
        int trunkHeight = 2 + random.Next(3);

        for (int y = 0; y < trunkHeight + 4; y++)
        {
            chunk.AddBlockAt(localX, worldY + y, localZ, log);
        }

        // The canopy is a five by five slab two blocks deep, starting where the trunk stops being bare.
        worldY += trunkHeight;
        localX -= CanopyMargin;
        localZ -= CanopyMargin;
        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                if (localX + x == trunkX && localZ + z == trunkZ)
                {
                    continue;
                }

                for (int y = 0; y < 2; y++)
                {
                    chunk.AddBlockAt(localX + x, worldY + y, localZ + z, leaves);
                }
            }
        }

        // A smaller cap on top, offset by one so the crown is not a flat square.
        localX += CanopyMargin;
        localZ++;
        worldY += 2;
        chunk.AddBlockAt(localX, worldY++, localZ, leaves);
        chunk.AddBlockAt(localX--, worldY--, localZ++, leaves);
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                chunk.AddBlockAt(localX + x, worldY + y, localZ, leaves);
            }
        }

        localX++;
        localZ++;
        chunk.AddBlockAt(localX, worldY++, localZ, leaves);
        chunk.AddBlockAt(localX, worldY, localZ, leaves);
    }
}
