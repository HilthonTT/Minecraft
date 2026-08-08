using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// The cold slopes: pine forest low down, thinning as the ground climbs, and nothing at all on the bare snow
/// of the summits.
/// </summary>
public sealed class SnowyDecorator : IDecorator
{
    private readonly PineTreeGenerator _pineTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);

        // Above the snow line the ground is bare snow, and the peaks are meant to look it.
        if (ground == BlockRegistry.Snow)
        {
            return;
        }

        if (ground == BlockRegistry.Stone)
        {
            if (random.Next(500) == 1)
            {
                SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.Stone, worldY, localX, localZ, random);
            }

            return;
        }

        if (ground != BlockRegistry.SnowyGrass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(45) == 1)
        {
            _pineTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
        else if (random.Next(80) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(600) == 1)
        {
            SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.MossyCobblestone, worldY, localX, localZ, random);
        }
    }
}
