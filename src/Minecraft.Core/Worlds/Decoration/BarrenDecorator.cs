using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class BarrenDecorator : IDecorator
{
    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        if (random.Next(300) == 1)
        {
            int cactusHeight = 2 + random.Next(3);
            for (int y = worldY; y < worldY + cactusHeight; y++)
            {
                chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(BlockRegistry.Cactus));
            }
        }
        else if (random.Next(200) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.DeadBush));
        }
    }
}
