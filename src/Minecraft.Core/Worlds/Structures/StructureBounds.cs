namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// The footprint of a structure or one of its pieces in world coordinates, inclusive on both ends. Height is
/// left out: a structure is placed one chunk column at a time, so only the horizontal extent decides which
/// chunks it reaches into.
/// </summary>
public readonly record struct StructureBounds(int MinX, int MinZ, int MaxX, int MaxZ)
{
    public int Width => MaxX - MinX + 1;

    public int Depth => MaxZ - MinZ + 1;

    public int CenterX => (MinX + MaxX) / 2;

    public int CenterZ => (MinZ + MaxZ) / 2;

    /// <summary>A box of the given size centred on a position, rounded down on the even sides.</summary>
    public static StructureBounds FromCenter(int centerX, int centerZ, int width, int depth)
    {
        int minX = centerX - (width / 2);
        int minZ = centerZ - (depth / 2);
        return new StructureBounds(minX, minZ, minX + width - 1, minZ + depth - 1);
    }

    public StructureBounds Expand(int blocks)
    {
        return new StructureBounds(MinX - blocks, MinZ - blocks, MaxX + blocks, MaxZ + blocks);
    }

    public bool Contains(int worldX, int worldZ)
    {
        return worldX >= MinX && worldX <= MaxX && worldZ >= MinZ && worldZ <= MaxZ;
    }

    /// <summary>Whether any part of this footprint falls inside the given chunk.</summary>
    public bool IntersectsChunk(int chunkX, int chunkZ)
    {
        int chunkMinX = chunkX * 16;
        int chunkMinZ = chunkZ * 16;
        return MinX <= chunkMinX + 15 && MaxX >= chunkMinX && MinZ <= chunkMinZ + 15 && MaxZ >= chunkMinZ;
    }

    public bool Intersects(StructureBounds other)
    {
        return MinX <= other.MaxX && MaxX >= other.MinX && MinZ <= other.MaxZ && MaxZ >= other.MinZ;
    }
}
