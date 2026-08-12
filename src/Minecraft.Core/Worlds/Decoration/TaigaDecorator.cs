using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Close pine forest: denser in trees than anything else in the world, with little under them but moss,
/// mushrooms and the odd boulder. What undergrowth there is stays low, since the point of a taiga next to a
/// forest is that the trunks are what you see.
/// </summary>
public sealed class TaigaDecorator : IDecorator
{
    private readonly PineTreeGenerator _pineTreeGenerator = new();

    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
        Block ground = SurfaceFeatures.GetGroundAt(chunk, worldY, localX, localZ);
        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt)
        {
            return;
        }

        if (random.Next(28) == 1)
        {
            _pineTreeGenerator.GenerateTreeAt(chunk, localX, worldY, localZ, random);
        }
        else if (random.Next(10) == 1)
        {
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(BlockRegistry.GrassBlade));
        }
        else if (random.Next(220) == 1)
        {
            // Mushrooms do well in the shade a close wood casts, so they are commoner here than out in an
            // open forest.
            Block mushroom = random.Next(2) == 0 ? BlockRegistry.RedMushroom : BlockRegistry.BrownMushroom;
            chunk.AddBlockAt(localX, worldY, localZ, BlockRegistry.GetState(mushroom));
        }
        else if (random.Next(800) == 1)
        {
            SurfaceFeatures.PlaceBoulder(chunk, BlockRegistry.MossyCobblestone, worldY, localX, localZ, random);
        }
    }
}
