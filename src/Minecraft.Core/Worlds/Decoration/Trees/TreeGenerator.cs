using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

/// <summary>
/// Shared groundwork for the trees that grow as a trunk with discs of leaves stacked around it.
/// <para>
/// A tree is written into one chunk and no further. Its neighbours are not loaded while it is being
/// generated, so a trunk standing near enough to an edge that its canopy would reach over is refused rather
/// than clipped, which would leave half a tree with a flat side.
/// </para>
/// </summary>
public abstract class TreeGenerator : ITreeGenerator
{
    /// <summary>How far the widest part of the canopy reaches out from the trunk.</summary>
    protected abstract int CanopyRadius { get; }

    /// <summary>The most blocks this tree can ever occupy above the ground it stands on.</summary>
    protected abstract int MaxHeight { get; }

    public void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ, Random random)
    {
        if (localX < CanopyRadius || localX >= 16 - CanopyRadius ||
            localZ < CanopyRadius || localZ >= 16 - CanopyRadius)
        {
            return;
        }

        if (worldY + MaxHeight >= Constants.MAX_BUILD_HEIGHT)
        {
            return;
        }

        Grow(chunk, localX, worldY, localZ, random);
    }

    /// <param name="worldY">The first free block above the ground, where the foot of the trunk goes.</param>
    protected abstract void Grow(Chunk chunk, int localX, int worldY, int localZ, Random random);

    protected static void PlaceTrunk(Chunk chunk, BlockState log, int localX, int worldY, int localZ, int height)
    {
        for (int y = 0; y < height; y++)
        {
            chunk.AddBlockAt(localX, worldY + y, localZ, log);
        }
    }

    /// <summary>
    /// Lays a flat square of leaves around the trunk. Nothing already standing is disturbed, so the trunk
    /// keeps showing through the middle of its own canopy.
    /// </summary>
    /// <param name="cutCorners">
    /// Leaves the four corners of the square out, which rounds the canopy off. Worth doing for anything wider
    /// than a single block, where a full square reads as a slab.
    /// </param>
    protected static void PlaceLeafDisc(
        Chunk chunk,
        BlockState leaves,
        int centreX,
        int worldY,
        int centreZ,
        int radius,
        bool cutCorners)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                if (cutCorners && Math.Abs(dx) == radius && Math.Abs(dz) == radius)
                {
                    continue;
                }

                if (chunk.GetBlockAt(centreX + dx, worldY, centreZ + dz).GetBlock() != BlockRegistry.Air)
                {
                    continue;
                }

                chunk.AddBlockAt(centreX + dx, worldY, centreZ + dz, leaves);
            }
        }
    }
}
