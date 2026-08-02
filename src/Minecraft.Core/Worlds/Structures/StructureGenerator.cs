using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Structures.Villages;

namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// Decides where structures stand and builds the parts of them that fall inside a chunk.
/// <para>
/// The world is cut into square regions, and each region gets at most one village, at a position drawn from
/// the region's own seed. That keeps villages roughly evenly spread without any two of them ever being able
/// to land on top of each other, and it means the question "is there a village near this chunk" can be
/// answered by looking at a handful of regions rather than by searching the world.
/// </para>
/// </summary>
public sealed class StructureGenerator
{
    /// <summary>The side of a region in chunks, which is how far apart two villages are on average.</summary>
    private const int VillageRegionSizeInChunks = 22;

    /// <summary>
    /// How much of a region a village is kept away from the far edge of. Two villages in neighbouring regions
    /// are always at least this many chunks apart, however their positions are drawn.
    /// </summary>
    private const int VillageSeparationInChunks = 8;

    /// <summary>Mixed into the seed so villages do not land wherever some later structure would.</summary>
    private const uint VillageSalt = 0x5645_4C4Cu;

    private readonly int _seed;

    public StructureGenerator(int seed)
    {
        _seed = seed;
    }

    /// <summary>
    /// Builds every structure that reaches into the given chunk.
    /// </summary>
    /// <param name="chunk">
    /// A chunk that has been filled with terrain, carved and decorated. Structures come last so that they
    /// clear away the trees and undergrowth standing where they are built.
    /// </param>
    /// <param name="terrain">Where the ground is, including in the chunks this structure spills into.</param>
    public void PlaceStructuresIn(Chunk chunk, ITerrainSampler terrain)
    {
        // Shared by every structure of this chunk, and dropped with it. Laying out a village walks over the
        // same columns repeatedly, and nearly all of them belong to other chunks.
        var cachedTerrain = new CachedTerrainSampler(terrain);
        var writer = new StructureWriter(chunk);

        foreach (Village village in FindVillagesNear(chunk.GridX, chunk.GridZ, cachedTerrain))
        {
            village.PlaceInto(writer);
        }
    }

    private IEnumerable<Village> FindVillagesNear(int chunkX, int chunkZ, ITerrainSampler terrain)
    {
        // A village centred this many chunks away can still reach the chunk being generated.
        int reachInChunks = (Village.MaxRadiusInBlocks / 16) + 1;

        int minRegionX = FloorDiv(chunkX - reachInChunks, VillageRegionSizeInChunks);
        int maxRegionX = FloorDiv(chunkX + reachInChunks, VillageRegionSizeInChunks);
        int minRegionZ = FloorDiv(chunkZ - reachInChunks, VillageRegionSizeInChunks);
        int maxRegionZ = FloorDiv(chunkZ + reachInChunks, VillageRegionSizeInChunks);

        for (int regionX = minRegionX; regionX <= maxRegionX; regionX++)
        {
            for (int regionZ = minRegionZ; regionZ <= maxRegionZ; regionZ++)
            {
                Village? village = TryCreateVillageInRegion(regionX, regionZ, chunkX, chunkZ, terrain);
                if (village is not null)
                {
                    yield return village;
                }
            }
        }
    }

    private Village? TryCreateVillageInRegion(
        int regionX,
        int regionZ,
        int chunkX,
        int chunkZ,
        ITerrainSampler terrain)
    {
        int regionSeed = GetStructureSeed(_seed, regionX, regionZ, VillageSalt);
        var random = new Random(regionSeed);

        const int offsetRange = VillageRegionSizeInChunks - VillageSeparationInChunks;
        int originChunkX = (regionX * VillageRegionSizeInChunks) + random.Next(offsetRange);
        int originChunkZ = (regionZ * VillageRegionSizeInChunks) + random.Next(offsetRange);

        // Centred on the chunk rather than on its corner, so the well is not sat on a chunk border.
        int centerX = (originChunkX * 16) + 8;
        int centerZ = (originChunkZ * 16) + 8;

        // The widest a village could possibly be. Checked before laying one out, because the region grid is
        // coarse enough that most of the candidates it turns up are nowhere near this chunk, and working out
        // where their houses go would be thrown away.
        var reach = StructureBounds.FromCenter(
            centerX,
            centerZ,
            (Village.MaxRadiusInBlocks * 2) + 1,
            (Village.MaxRadiusInBlocks * 2) + 1);

        if (!reach.IntersectsChunk(chunkX, chunkZ))
        {
            return null;
        }

        Village? village = Village.TryCreate(regionSeed, centerX, centerZ, terrain);
        return village?.Bounds.IntersectsChunk(chunkX, chunkZ) is true ? village : null;
    }

    /// <summary>
    /// Division that rounds towards negative infinity, so that regions are the same size on both sides of the
    /// origin. Plain integer division rounds towards zero, which would make the two regions either side of it
    /// share one row of chunks.
    /// </summary>
    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        return value % divisor < 0 ? quotient - 1 : quotient;
    }

    /// <summary>
    /// Mixes the world seed with a region position into a seed for whatever stands in that region.
    /// <para>
    /// Deliberately not <see cref="HashCode.Combine{T1, T2, T3}"/>: that is randomised per process, so a world
    /// would put its villages somewhere else every time it was loaded, and the half of a village stored on
    /// disk would stop lining up with the half regenerated around it.
    /// </para>
    /// </summary>
    private static int GetStructureSeed(int seed, int regionX, int regionZ, uint salt)
    {
        unchecked
        {
            uint hash = (uint)seed ^ salt;
            hash = (hash ^ (uint)regionX) * 2654435761u;
            hash = (hash ^ (uint)regionZ) * 2246822519u;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)hash;
        }
    }
}
