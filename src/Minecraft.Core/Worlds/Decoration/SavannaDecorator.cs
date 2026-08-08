using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Dry grassland: heavy on grass and light on everything else, with single trees standing well apart and dead
/// bushes on the bare stretches between them.
/// </summary>
public sealed class SavannaDecorator : IDecorator
{
    private readonly OakTreeGenerator _oakTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);

        if (ground == BlockRegistry.Sand)
        {
            if (random.Next(120) == 1)
            {
                chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.DeadBush));
            }

            return;
        }

        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(3) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(700) == 1)
        {
            _oakTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
        else if (random.Next(900) == 1)
        {
            SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.Cobblestone, worldY, localX, localZ, random);
        }
    }
}
