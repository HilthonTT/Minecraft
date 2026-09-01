using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class ForestDecorator : IDecorator
{
    private readonly OakTreeGenerator _oakTreeGenerator = new();
    private readonly BirchTreeGenerator _birchTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);
        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(8) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(200) == 1)
        {
            Block flower = random.Next(2) == 0 ? BlockRegistry.Flower : BlockRegistry.Dandelion;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(flower));
        }
        else if (random.Next(500) == 1)
        {
            Block mushroom = random.Next(2) == 0 ? BlockRegistry.RedMushroom : BlockRegistry.BrownMushroom;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(mushroom));
        }
        else if (random.Next(40) == 1)
        {
            if (random.Next(4) == 0)
            {
                _birchTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
            }
            else
            {
                _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
            }
        }
    }
}
