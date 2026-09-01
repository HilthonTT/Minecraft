using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Structures.Villages;

namespace Minecraft.Core.Worlds.Structures;

public sealed class StructureGenerator
{
    private const int VillageRegionSizeInChunks = 22;

    private const int VillageSeparationInChunks = 8;

    private const uint VillageSalt = 0x5645_4C4Cu;

    private readonly int _seed;

    public StructureGenerator(int seed)
    {
        _seed = seed;
    }

    public void PlaceStructuresIn(Chunk chunk, ITerrainSampler terrain)
    {
        var cachedTerrain = new CachedTerrainSampler(terrain);
        var writer = new StructureWriter(chunk);

        foreach (Village village in FindVillagesNear(chunk.GridX, chunk.GridZ, cachedTerrain))
        {
            village.PlaceInto(writer);
        }
    }

    private IEnumerable<Village> FindVillagesNear(int chunkX, int chunkZ, ITerrainSampler terrain)
    {
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

        int centerX = (originChunkX * 16) + 8;
        int centerZ = (originChunkZ * 16) + 8;

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

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        return value % divisor < 0 ? quotient - 1 : quotient;
    }

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
