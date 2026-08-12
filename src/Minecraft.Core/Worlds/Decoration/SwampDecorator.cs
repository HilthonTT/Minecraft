using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>
/// Marsh: reeds along every waterline, mushrooms and tall grass over the ground between the pools, and oaks
/// standing singly rather than in a wood. The banks are patched with clay, which is what a swamp has instead
/// of the sand a proper shore would leave.
/// </summary>
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

        // Reeds only grow within reach of the water, which is what draws the edge of every pool rather than
        // scattering them evenly over the marsh.
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

    /// <summary>
    /// Whether any of the four cells around this one holds water. Only the chunk's own columns are asked:
    /// a neighbouring chunk is not loaded while this one is being decorated, and reeds missing from the
    /// outermost row of a chunk is a far smaller thing than reading blocks that are not there.
    /// </summary>
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
