namespace Minecraft.Core.Worlds.Structures;

public readonly record struct StructureBounds(int MinX, int MinZ, int MaxX, int MaxZ)
{
    public int Width => MaxX - MinX + 1;

    public int Depth => MaxZ - MinZ + 1;

    public int CenterX => (MinX + MaxX) / 2;

    public int CenterZ => (MinZ + MaxZ) / 2;

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
