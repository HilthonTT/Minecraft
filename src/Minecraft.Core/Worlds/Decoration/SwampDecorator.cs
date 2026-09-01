using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class SwampDecorator : IDecorator
{
    private readonly OakTreeGenerator _oakTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);
        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt && ground != BlockRegistry.Clay)
        {
            return;
        }

        bool besideWater = IsBesideWater(chunk, worldY, localX, localZ);

        if (besideWater && random.Next(6) == 1)
        {
            int caneHeight = 1 + random.Next(3);
            for (int y = worldY; y < worldY + caneHeight; y++)
            {
                chunk.AddBlockAt(localX, y, localZ, BlockRegistry.GetState(BlockRegistry.SugarCane));
            }

            return;
        }

        if (besideWater && random.Next(4) == 1)
        {
            chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.Clay));
            return;
        }

        if (random.Next(4) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(90) == 1)
        {
            Block mushroom = random.Next(2) == 0 ? BlockRegistry.RedMushroom : BlockRegistry.BrownMushroom;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(mushroom));
        }
        else if (random.Next(300) == 1)
        {
            _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
    }

    private static bool IsBesideWater(Chunk chunk, int worldY, int localX, int localZ)
    {
        if (localX == 0 || localX == 15 || localZ == 0 || localZ == 15)
        {
            return false;
        }

        return IsWater(chunk, worldY - 1, localX - 1, localZ)
               || IsWater(chunk, worldY - 1, localX + 1, localZ)
               || IsWater(chunk, worldY - 1, localX, localZ - 1)
               || IsWater(chunk, worldY - 1, localX, localZ + 1);
    }

    private static bool IsWater(Chunk chunk, int worldY, int localX, int localZ)
    {
        return chunk.GetBlockAt(localX, worldY, localZ).GetBlock().IsLiquid;
    }
}
