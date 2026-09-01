using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Structures;

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

    public StructureBounds Bounds => new(_chunkMinX, _chunkMinZ, _chunkMinX + 15, _chunkMinZ + 15);

    public void SetBlock(int worldX, int worldY, int worldZ, BlockState blockState)
    {
        if (!IsWritable(worldX, worldY, worldZ))
        {
            return;
        }

        _chunk.AddBlockAt(worldX - _chunkMinX, worldY, worldZ - _chunkMinZ, blockState);
    }

    public void SetBlock(int worldX, int worldY, int worldZ, Block block)
    {
        if (block == BlockRegistry.Air)
        {
            Clear(worldX, worldY, worldZ);
            return;
        }

        SetBlock(worldX, worldY, worldZ, BlockRegistry.GetState(block));
    }

    public void Clear(int worldX, int worldY, int worldZ)
    {
        if (!IsWritable(worldX, worldY, worldZ))
        {
            return;
        }

        _chunk.RemoveBlockAt(worldX - _chunkMinX, worldY, worldZ - _chunkMinZ);
    }

    public void ClearColumn(int worldX, int worldZ, int fromY, int toY)
    {
        for (int y = fromY; y <= toY; y++)
        {
            Clear(worldX, y, worldZ);
        }
    }

    public void FillColumn(int worldX, int worldZ, int fromY, int toY, Block block)
    {
        BlockState state = BlockRegistry.GetState(block);
        for (int y = fromY; y <= toY; y++)
        {
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
