using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// Lets a structure build in world coordinates while only one chunk of it is actually being generated.
/// <para>
/// Everything falling outside that chunk is dropped rather than rejected: the same structure is built again,
/// unchanged, for each chunk it covers, and each of those runs keeps a different slice of it. That is what
/// lets a building span a chunk border without any chunk ever having to touch its neighbour.
/// </para>
/// </summary>
public sealed class StructureWriter
{
    private readonly Chunk _chunk;
    private readonly int _chunkMinX;
    private readonly int _chunkMinZ;

    public StructureWriter(Chunk chunk)
    {
        _chunk = chunk;
        _chunkMinX = chunk.GridX * 16;
        _chunkMinZ = chunk.GridZ * 16;
    }

    public int ChunkX => _chunk.GridX;

    public int ChunkZ => _chunk.GridZ;

    /// <summary>The part of the world this writer accepts, which is the chunk it was opened on.</summary>
    public StructureBounds Bounds => new(_chunkMinX, _chunkMinZ, _chunkMinX + 15, _chunkMinZ + 15);

    /// <summary>Places a block, ignoring anything outside the chunk or the build height.</summary>
    public void SetBlock(int worldX, int worldY, int worldZ, BlockState blockState)
    {
        if (!IsWritable(worldX, worldY, worldZ))
        {
            return;
        }

        _chunk.AddBlockAt(worldX - _chunkMinX, worldY, worldZ - _chunkMinZ, blockState);
    }

    /// <summary>Places a block, or clears the position when given air.</summary>
    public void SetBlock(int worldX, int worldY, int worldZ, Block block)
    {
        if (block == BlockRegistry.Air)
        {
            Clear(worldX, worldY, worldZ);
            return;
        }

        SetBlock(worldX, worldY, worldZ, BlockRegistry.GetState(block));
    }

    /// <summary>Empties a position, ignoring anything outside the chunk or the build height.</summary>
    public void Clear(int worldX, int worldY, int worldZ)
    {
        if (!IsWritable(worldX, worldY, worldZ))
        {
            return;
        }

        _chunk.RemoveBlockAt(worldX - _chunkMinX, worldY, worldZ - _chunkMinZ);
    }

    /// <summary>Empties a vertical run of a column, both ends included.</summary>
    public void ClearColumn(int worldX, int worldZ, int fromY, int toY)
    {
        for (int y = fromY; y <= toY; y++)
        {
            Clear(worldX, y, worldZ);
        }
    }

    /// <summary>Fills a vertical run of a column with one block, both ends included.</summary>
    public void FillColumn(int worldX, int worldZ, int fromY, int toY, Block block)
    {
        BlockState state = BlockRegistry.GetState(block);
        for (int y = fromY; y <= toY; y++)
        {
            // A fresh state per block for the few blocks that carry their own data, since a shared one would
            // be seen at every position it was placed at.
            SetBlock(worldX, y, worldZ, block.HasCustomState ? BlockRegistry.GetState(block) : state);
        }
    }

    private bool IsWritable(int worldX, int worldY, int worldZ)
    {
        return worldY >= 0
               && worldY < Constants.MAX_BUILD_HEIGHT
               && worldX >= _chunkMinX
               && worldX <= _chunkMinX + 15
               && worldZ >= _chunkMinZ
               && worldZ <= _chunkMinZ + 15;
    }
}
