using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Trees;

public sealed class OakTreeGenerator : ITreeGenerator
{
    private readonly Random _random = new();

    public void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ)
    {
        if (localX > 2 && localX < 13 && localZ > 2 && localZ < 13)
        {
            BlockState leaves = BlockRegistry.GetState(BlockRegistry.OakLeaves);

            int trunckX = localX;
            int trunckZ = localZ;
            int r = 2 + _random.Next(3);
            for (int yy = 0; yy < r + 4; yy++)
            {
                chunk.AddBlockAt(localX, worldY + yy, localZ, BlockRegistry.GetState(BlockRegistry.OakLog));
            }
            worldY += r;
            localX -= 2;
            localZ -= 2;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (localX + i == trunckX && localZ + j == trunckZ)
                    {
                        continue;
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        chunk.AddBlockAt(localX + i, worldY + k, localZ + j, leaves);
                    }
                }
            }
            localX += 2;
            localZ++;
            worldY += 2;
            chunk.AddBlockAt(localX, worldY++, localZ, leaves);
            chunk.AddBlockAt(localX--, worldY--, localZ++, leaves);
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    chunk.AddBlockAt(localX + i, worldY + k, localZ, leaves);
                }
            }
            localX++;
            localZ++;
            chunk.AddBlockAt(localX, worldY++, localZ, leaves);
            chunk.AddBlockAt(localX, worldY, localZ, leaves);
        }
    }
}