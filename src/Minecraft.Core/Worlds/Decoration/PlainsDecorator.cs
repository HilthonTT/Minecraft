using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class PlainsDecorator : IDecorator
{
    private readonly OakTreeGenerator _oakTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);
        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(4) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(60) == 1)
        {
            Block flower = random.Next(2) == 0 ? BlockRegistry.Flower : BlockRegistry.Dandelion;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(flower));
        }
        else if (random.Next(400) == 1)
        {
            _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
    }
}
