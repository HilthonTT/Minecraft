using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Cuts long, narrow gorges down through a chunk that has already been filled with terrain.
/// <para>
/// A ravine is the line where one low frequency field crosses zero, the same way a river is: that line
/// wanders for hundreds of blocks and never stops abruptly, which is what a gorge has to do — a canyon that
/// ended in a wall halfway along would read as a hole rather than as something the ground was split by. A
/// second, much broader field decides which stretches of that line are actually opened, so most of the world
/// has none and the ones there are stand well apart.
/// </para>
/// <para>
/// Like the caves, whether a block is cut depends on nothing but its world position and the seeded noise, so
/// a gorge lines up across chunk borders without the carver ever looking outside the chunk it is given.
/// </para>
/// </summary>
public sealed class RavineCarver
{
    /// <summary>How far the line a ravine follows wanders, and over what distance.</summary>
    private const float ChannelDetail = 0.0016F;
    private const int ChannelOctaves = 2;
    private const float ChannelDomainOffset = 5717.61F;

    /// <summary>
    /// How far either side of the zero line the gorge reaches, in units of the field. This is what sets its
    /// width, and it is deliberately a fraction of the river's: a ravine is a split in the ground rather
    /// than a valley, and the drop is what makes it rather than the span.
    /// </summary>
    private const float ChannelWidth = 0.014F;

    /// <summary>
    /// Which stretches of the world hold a ravine at all. A slow field, opened over the part of its range
    /// just above the middle: Perlin noise bunches hard around its middle, so a bar set out where it looks
    /// selective is one the field almost never clears. What keeps gorges rare is the line they follow, which
    /// is thin; the region only decides where along that line the ground actually gave way.
    /// </summary>
    private const float RegionDetail = 0.0022F;
    private const float RegionDomainOffset = 3391.07F;
    private const float RegionThreshold = 0.58F;
    private const float RegionFullyOpen = 0.66F;

    /// <summary>
    /// How deep a gorge cuts below the surface it opens in, and the lowest it will ever reach. The floor is
    /// kept above where the deep caverns run, so a ravine tends to break into them from above rather than
    /// bottoming out below everything.
    /// </summary>
    private const int MaxDepth = 52;
    private const int LowestFloorY = 14;

    /// <summary>
    /// How far the floor wanders along the length of a gorge, and over what distance. Without it a ravine
    /// bottoms out at the same height for its whole run and reads as a trench that was dug rather than as
    /// ground that gave way.
    /// </summary>
    private const float FloorDetail = 0.010F;
    private const float FloorDomainOffset = 8123.93F;
    private const int FloorVariation = 14;

    /// <summary>
    /// How much of its width a ravine keeps at the floor. Well under half, so the walls lean in as they go
    /// down and what is left at the bottom is a crack rather than a corridor.
    /// </summary>
    private const float FloorWidthShare = 0.28F;

    /// <summary>
    /// How far below the surface the walls have finished leaning out. Over this depth the gorge widens from
    /// its floor to its full span, which is what gives it a mouth wider than its base.
    /// </summary>
    private const float FlareDepth = 26F;

    /// <summary>
    /// Removes every block of <paramref name="chunk"/> that falls inside a ravine.
    /// </summary>
    /// <param name="chunk">A chunk that has been filled with terrain but not yet decorated.</param>
    /// <param name="surfaceHeights">
    /// The height of the terrain surface of each column of the chunk, indexed by chunk local x and z.
    /// </param>
    public void Carve(Chunk chunk, int[,] surfaceHeights)
    {
        const int chunkDim = 16;

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int worldX = chunk.GridX * chunkDim + localX;
                int worldZ = chunk.GridZ * chunkDim + localZ;

                // Both are constant down a column, so they are worked out once here rather than per block.
                float region = RegionOpennessAt(worldX, worldZ);
                if (region <= 0F)
                {
                    continue;
                }

                int surfaceY = surfaceHeights[localX, localZ];
                int floorY = GetFloorY(worldX, worldZ, surfaceY);
                if (floorY >= surfaceY)
                {
                    continue;
                }

                for (int y = floorY; y <= surfaceY; y++)
                {
                    if (!IsInsideRavine(worldX, y, worldZ, surfaceY, region))
                    {
                        continue;
                    }

                    if (chunk.GetBlockAt(localX, y, localZ).GetBlock() == BlockRegistry.Air)
                    {
                        continue;
                    }

                    chunk.RemoveBlockAt(localX, y, localZ);
                }
            }
        }
    }

    /// <summary>
    /// Whether a block falls inside the gorge, which it does when it is near enough to the line the gorge
    /// follows — near enough being a question of how far down it is, since the walls lean in towards the
    /// floor.
    /// </summary>
    private static bool IsInsideRavine(int worldX, int worldY, int worldZ, int surfaceY, float region)
    {
        // One well below the surface, falling to zero at the surface the gorge opens in.
        float depthShare = Math.Clamp((surfaceY - worldY) / FlareDepth, 0F, 1F);

        float width = ChannelWidth * region * (FloorWidthShare + ((1F - FloorWidthShare) * (1F - depthShare)));

        float channel = Noise2DPerlinOctave.Noise(
            worldZ * ChannelDetail + ChannelDomainOffset,
            worldX * ChannelDetail + ChannelDomainOffset,
            ChannelOctaves);

        return MathF.Abs(channel) < width;
    }

    /// <summary>Where the bottom of the gorge lies at a column, wandering along its length.</summary>
    private static int GetFloorY(int worldX, int worldZ, int surfaceY)
    {
        float wander = Noise2DPerlin.Noise01(
            worldX * FloorDetail + FloorDomainOffset,
            worldZ * FloorDetail + FloorDomainOffset);

        int depth = MaxDepth - (int)(wander * FloorVariation);
        return Math.Max(LowestFloorY, surfaceY - depth);
    }

    /// <summary>
    /// How far the ground at a column is given over to a ravine, from nothing over most of the world to one
    /// in the middle of a stretch that holds one.
    /// </summary>
    private static float RegionOpennessAt(int worldX, int worldZ)
    {
        float region = Noise2DPerlin.Noise01(
            worldX * RegionDetail + RegionDomainOffset,
            worldZ * RegionDetail + RegionDomainOffset);

        return Math.Clamp((region - RegionThreshold) / (RegionFullyOpen - RegionThreshold), 0F, 1F);
    }
}
