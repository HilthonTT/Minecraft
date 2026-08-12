using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Open snow: bare for the most part, with pines standing alone or in small stands and drifts of snow lying
/// between them. What makes it read as a plain rather than a forest is how much of it is nothing at all.
/// </summary>
public sealed class SnowyPlainsDecorator : IDecorator
{
    private readonly PineTreeGenerator _pineTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);
        if (ground != BlockRegistry.SnowyGrass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(200) == 1)
        {
            _pineTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
        else if (random.Next(120) == 1)
        {
            // A drift: the ground here has been under snow long enough to have become it.
            chunk.AddBlockAt(localX, worldY - 1, localZ, BlockRegistry.GetState(BlockRegistry.Snow));
        }
        else if (random.Next(900) == 1)
        {
            SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.Stone, worldY, localX, localZ, random);
        }
    }
}
