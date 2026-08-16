using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Hangs springs off the drops in a chunk, so that a gorge or a cliff in wet country has water coming down
/// its wall. What it looks for is a column of open air against a lip: mostly that is the side of a ravine,
/// which is what has just been cut through the chunk, and sometimes the mouth of a cave or a step in the
/// terrain steep enough to fall off.
/// <para>
/// The whole fall is written out — a spring at the lip, water on its way down the face, and a pool at the
/// foot of it — rather than a source left for the water physics to run. Terrain is written into a chunk that
/// is not part of a world yet, so nothing generated is ever ticked; and a chunk nobody has touched is thrown
/// away and regenerated rather than stored, so what is generated has to be what the physics would have
/// settled on anyway. It is: a spring feeds the cell below it, water coming down a face feeds the next one,
/// and both hand on what a source would, which is exactly the column written here.
/// </para>
/// </summary>
public sealed class WaterfallGenerator
{
    /// <summary>
    /// How far the ground has to fall away beside a column before water will run off it. Tall enough that
    /// the drop reads as a wall rather than as a step in a hillside.
    /// </summary>
    private const int MinDrop = 6;

    /// <summary>
    /// The longest fall that will be written, which is also as far down a wall as the search for a floor
    /// goes. Past this the water is falling into the dark, and a ribbon that long reads as a line drawn down
    /// the wall rather than as a spring coming off it.
    /// </summary>
    private const int MaxDrop = 64;

    /// <summary>How wet a biome has to be to hold a spring. Nothing runs down a cliff in a desert.</summary>
    private const double MinMoisture = 0.40D;

    /// <summary>
    /// How often a wall that could hold one actually does, and how many are allowed in a single chunk. The
    /// odds are short because what they are applied to is already rare — a wall this tall is the side of a
    /// gorge, and there are only so many of those — and the cap is what stops one gorge from being lined
    /// with them.
    /// </summary>
    private const int SpringOdds = 3;
    private const int MaxSpringsPerChunk = 2;

    /// <summary>The four ways the ground can fall away from a column.</summary>
    private static readonly (int X, int Z)[] _neighbourOffsets = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    /// <summary>
    /// Writes whatever springs this chunk has earned into it.
    /// </summary>
    /// <param name="chunk">A chunk whose caves have been carved and whose seas have been filled.</param>
    /// <param name="surfaceHeights">The terrain surface of each column, indexed by chunk local x and z.</param>
    /// <param name="surfaceBiomes">The biome of each column, indexed the same way.</param>
    /// <param name="seaLevel">The waterline, below which a fall would only be running into the sea.</param>
    /// <param name="random">Seeded per chunk, so a chunk always grows the same springs.</param>
    public void PlaceWaterfallsIn(
        Chunk chunk,
        int[,] surfaceHeights,
        Biome[,] surfaceBiomes,
        int seaLevel,
        Random random)
    {
        int placed = 0;

        // The outermost row is left alone: a fall goes down the column beside the one it runs off, and the
        // neighbour of an edge column belongs to a chunk that is not loaded to be written into.
        for (int localX = 1; localX < 15 && placed < MaxSpringsPerChunk; localX++)
        {
            for (int localZ = 1; localZ < 15 && placed < MaxSpringsPerChunk; localZ++)
            {
                if (surfaceBiomes[localX, localZ].Moisture < MinMoisture)
                {
                    continue;
                }

                // The wall is looked for before the dice are rolled. Rolling first would spend the odds on
                // the flat ground that most of a chunk is, and leave a wall that could carry a fall dry
                // because its column happened not to come up.
                if (FindFall(chunk, surfaceHeights, localX, localZ, seaLevel) is not (int faceX, int faceZ, int footY))
                {
                    continue;
                }

                if (random.Next(SpringOdds) != 0)
                {
                    continue;
                }

                WriteFall(chunk, faceX, faceZ, footY, surfaceHeights[localX, localZ]);
                placed++;
            }
        }
    }

    /// <summary>
    /// The wall beside this column that water could come off, and how far down it the water would land.
    /// <para>
    /// Read off the blocks themselves rather than off the terrain heights, since by now the interesting
    /// walls are the ones the carvers left: the side of a ravine is a drop of thirty blocks that the height
    /// map knows nothing about.
    /// </para>
    /// </summary>
    private static (int X, int Z, int FootY)? FindFall(
        Chunk chunk,
        int[,] surfaceHeights,
        int localX,
        int localZ,
        int seaLevel)
    {
        int lipY = surfaceHeights[localX, localZ];

        // Nothing runs off a lip that stands under water, and nothing runs off one that is not there: a
        // column the carvers opened has no ground at its own surface to be the edge of anything.
        if (lipY <= seaLevel + 1 || IsAir(chunk, localX, lipY, localZ))
        {
            return null;
        }

        foreach ((int offsetX, int offsetZ) in _neighbourOffsets)
        {
            int faceX = localX + offsetX;
            int faceZ = localZ + offsetZ;

            // The wall starts where the ground beside the lip is already gone.
            if (!IsAir(chunk, faceX, lipY, faceZ))
            {
                continue;
            }

            int footY = FindFloorBelow(chunk, faceX, faceZ, lipY);
            if (footY < 0 || lipY - footY < MinDrop)
            {
                continue;
            }

            return (faceX, faceZ, footY);
        }

        return null;
    }

    /// <summary>
    /// The first solid block below the lip in the given column, which is what the water lands on.
    /// </summary>
    /// <returns>
    /// Its height, or -1 where the drop runs on past what a fall is allowed to cover, or where something
    /// other than air is in the way — water already standing in the wall, most of all, which would mean the
    /// spring was running into a pool that is already there.
    /// </returns>
    private static int FindFloorBelow(Chunk chunk, int faceX, int faceZ, int lipY)
    {
        int lowest = Math.Max(0, lipY - MaxDrop);

        for (int y = lipY - 1; y >= lowest; y--)
        {
            Block block = chunk.GetBlockAt(faceX, y, faceZ).GetBlock();

            if (block == BlockRegistry.Air)
            {
                continue;
            }

            return block.IsLiquid ? -1 : y;
        }

        return -1;
    }

    private static bool IsAir(Chunk chunk, int localX, int worldY, int localZ)
    {
        return chunk.GetBlockAt(localX, worldY, localZ).GetBlock() == BlockRegistry.Air;
    }

    /// <summary>
    /// Writes the fall itself: a spring at the lip, the ribbon of water down the face, and the pool it
    /// gathers into at the foot.
    /// </summary>
    private static void WriteFall(Chunk chunk, int faceX, int faceZ, int footY, int lipY)
    {
        BlockState source = BlockRegistry.GetState(BlockRegistry.Water);
        BlockState falling = BlockRegistry.GetState(BlockRegistry.WaterFalling);

        // The spring stands level with the top of the cliff it comes out of, so what is seen from below is
        // water leaving the lip rather than appearing halfway down.
        chunk.AddBlockAt(faceX, lipY, faceZ, source);

        for (int y = footY + 2; y < lipY; y++)
        {
            chunk.AddBlockAt(faceX, y, faceZ, falling);
        }

        // A source at the bottom rather than more falling water: what gathers at the foot of a fall is a
        // pool, and a pool that could dry up would leave the ground dry the moment anything disturbed it.
        chunk.AddBlockAt(faceX, footY + 1, faceZ, source);
    }
}
