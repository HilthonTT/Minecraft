using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

public sealed class Chunk
{
    public Dictionary<Vector3i, BlockState> TickableBlocks { get; } = [];

    public Dictionary<Vector3i, BlockState> LightSourceBlocks { get; } = [];

    public Section?[] Sections { get; } = new Section?[Constants.NUM_SECTIONS_IN_CHUNKS];

    public int GridX { get; private set; }

    public int GridZ { get; private set; }

    public LightMap LightMap { get; } = new();

    public int[,] TopMostBlocks { get; } = new int[16, 16];

    public bool IsDirty { get; private set; }

    public Chunk(int gridX, int gridZ)
    {
        GridX = gridX;
        GridZ = gridZ;
    }

    public Chunk()
    {
    }

    public void ResetAndAssign(int gridX, int gridZ)
    {
        TickableBlocks.Clear();
        LightSourceBlocks.Clear();

        for (int height = 0; height < Constants.NUM_SECTIONS_IN_CHUNKS; height++)
        {
            Section? section = Sections[height];
            if (section is null)
            {
                continue;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        section.RemoveBlockAt(x, y, z);
                    }
                }
            }

            if (!section.IsEmpty || !section.IsFullTransparent)
            {
                throw new InvalidOperationException("Invalid section reset. " + section);
            }
        }

        LightMap.CleanSunlightMap();

        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                TopMostBlocks[x, z] = 0;
            }
        }

        GridX = gridX;
        GridZ = gridZ;
        IsDirty = false;

        for (int height = 0; height < Constants.NUM_SECTIONS_IN_CHUNKS; height++)
        {
            Sections[height]?.ResetAndAssign(gridX, gridZ);
        }
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public override string ToString()
    {
        return "Chunk[" + GridX + "," + GridZ + "]";
    }

    public void Tick(float deltaTime, World world)
    {
        foreach (KeyValuePair<Vector3i, BlockState> tickable in TickableBlocks.ToArray())
        {
            tickable.Value.GetBlock().OnTick(tickable.Value, world, tickable.Key, deltaTime);
        }
    }

    public BlockState GetBlockAt(int localX, int worldY, int localZ)
    {
        return GetBlockAt(new Vector3i(localX, worldY, localZ));
    }

    public BlockState GetBlockAt(Vector3i localPos)
    {
        if (localPos.Y < 0 || localPos.Y >= Constants.MAX_BUILD_HEIGHT)
        {
            return BlockRegistry.GetState(BlockRegistry.Air);
        }

        Section? section = Sections[localPos.Y / 16];
        if (section is null)
        {
            return BlockRegistry.GetState(BlockRegistry.Air);
        }

        return section.GetBlockAt(localPos.X, localPos.Y & 15, localPos.Z)
               ?? BlockRegistry.GetState(BlockRegistry.Air);
    }

    public void RemoveBlockAt(int localX, int worldY, int localZ)
    {
        if (worldY < 0 || worldY >= Constants.MAX_BUILD_HEIGHT)
        {
            throw new ArgumentOutOfRangeException(
                nameof(worldY),
                worldY,
                "Removing block outside of the build height in chunk (" + GridX + ", " + GridZ + ")");
        }

        Section? section = Sections[worldY / 16];
        if (section is null)
        {
            return;
        }

        var blockPos = new Vector3i(localX + GridX * 16, worldY, localZ + GridZ * 16);

        section.RemoveBlockAt(localX, worldY & 15, localZ);
        TickableBlocks.Remove(blockPos);
        LightSourceBlocks.Remove(blockPos);
        IsDirty = true;

        if (TopMostBlocks[localX, localZ] == worldY)
        {
            TopMostBlocks[localX, localZ] = FindNewTopMostBlockAt(localX, localZ, worldY);
        }
    }

    private int FindNewTopMostBlockAt(int localX, int localZ, int startY)
    {
        for (int y = startY - 1; y >= 0; y--)
        {
            if (GetBlockAt(localX, y, localZ).GetBlock() != BlockRegistry.Air)
            {
                return y;
            }
        }

        return 0;
    }

    public void AddBlockAt(int localX, int worldY, int localZ, BlockState blockState)
    {
        if (worldY < 0 || worldY >= Constants.MAX_BUILD_HEIGHT)
        {
            throw new ArgumentOutOfRangeException(
                nameof(worldY),
                worldY,
                "Adding block outside of the build height in chunk (" + GridX + ", " + GridZ + ")");
        }

        Block block = blockState.GetBlock();
        if (block == BlockRegistry.Air)
        {
            throw new ArgumentException("Air cannot be added. Remove the block instead.", nameof(blockState));
        }

        int sectionHeight = worldY / 16;
        Section section = Sections[sectionHeight] ??= new Section(GridX, GridZ, (byte)sectionHeight);

        var worldPos = new Vector3i(localX + GridX * 16, worldY, localZ + GridZ * 16);
        section.AddBlockAt(localX, worldY & 15, localZ, blockState);
        IsDirty = true;

        if (block.IsTickable)
        {
            TickableBlocks[worldPos] = blockState;
        }
        else
        {
            TickableBlocks.Remove(worldPos);
        }

        if (blockState is ILightSource)
        {
            LightSourceBlocks[worldPos] = blockState;
        }
        else
        {
            LightSourceBlocks.Remove(worldPos);
        }

        if (TopMostBlocks[localX, localZ] < worldY)
        {
            TopMostBlocks[localX, localZ] = worldY;
        }
    }

    public uint GetLowestEmptySectionAfterEachOtherFromTop()
    {
        uint lowestSection = Constants.NUM_SECTIONS_IN_CHUNKS - 1;

        for (int i = Constants.NUM_SECTIONS_IN_CHUNKS - 1; i >= 0; i--)
        {
            Section? section = Sections[i];
            if (section is not null && !section.IsFullTransparent)
            {
                break;
            }

            lowestSection = (uint)i;
        }

        return lowestSection;
    }

    public int GetPayloadSize()
    {
        int size = 0;

        for (int i = 0; i < Constants.NUM_SECTIONS_IN_CHUNKS; i++)
        {
            size++;

            Section? section = Sections[i];
            if (section is null)
            {
                continue;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        size += sizeof(ushort);
                        size += section.GetBlockAt(x, y, z)?.PayloadSize() ?? 0;
                    }
                }
            }
        }

        return size;
    }
}
