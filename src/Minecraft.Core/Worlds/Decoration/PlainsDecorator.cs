using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Open grass with flowers through it and the occasional lone tree. Deliberately sparse in trees: what makes
/// plains read as plains next to a forest is being able to see across them.
/// </summary>
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
            // Two flowers, drawn separately so a meadow comes out mixed rather than all of one colour.
            Block flower = random.Next(2) == 0 ? BlockRegistry.Flower : BlockRegistry.Dandelion;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(flower));
        }
        else if (random.Next(400) == 1)
        {
            _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
    }
}
