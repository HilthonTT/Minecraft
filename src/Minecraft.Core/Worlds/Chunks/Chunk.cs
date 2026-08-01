using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Lighting;
using Minecraft.Core.Worlds.Sections;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

/// <summary>
/// A 16 by 16 column of the world spanning the full build height, stored as a stack of
/// <see cref="Section"/>s so that empty vertical space costs nothing.
/// </summary>
public sealed class Chunk
{
    /// <summary>Blocks in this chunk that need ticking, keyed by world position.</summary>
    public Dictionary<Vector3i, BlockState> TickableBlocks { get; } = [];

    /// <summary>Blocks in this chunk that emit light, keyed by world position.</summary>
    public Dictionary<Vector3i, BlockState> LightSourceBlocks { get; } = [];

    /// <summary>Indexed by section height. A null entry means that slice contains no blocks at all.</summary>
    public Section?[] Sections { get; } = new Section?[Constants.NUM_SECTIONS_IN_CHUNKS];

    public int GridX { get; private set; }

    public int GridZ { get; private set; }

    public LightMap LightMap { get; } = new();

    /// <summary>The height of the highest non air block in each column, used to speed up lighting.</summary>
    public int[,] TopMostBlocks { get; } = new int[16, 16];

    public Chunk(int gridX, int gridZ)
    {
        GridX = gridX;
        GridZ = gridZ;
    }

    /// <summary>Required by the chunk pool, which recycles instances through <see cref="ResetAndAssign"/>.</summary>
    public Chunk()
    {
    }

    /// <summary>
    /// Wipes this chunk and moves it to a new grid position. Sections are emptied rather than dropped, so a
    /// recycled chunk does not have to reallocate them.
    /// </summary>
    public void ResetAndAssign(int gridX, int gridZ)
    {
        TickableBlocks.Clear();
        LightSourceBlocks.Clear();

        for (int height = 0; height < Constants.NUM_SECTIONS_IN_CHUNKS; height++)
        {
            Section? section = Sections[height];
            if (section == null)
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

        for (int height = 0; height < Constants.NUM_SECTIONS_IN_CHUNKS; height++)
        {
            Sections[height]?.ResetAndAssign(gridX, gridZ);
        }
    }

    public override string ToString()
    {
        return "Chunk[" + GridX + "," + GridZ + "]";
    }

    public void Tick(float deltaTime, World world)
    {
        // Copied because a block's tick can add or remove tickable blocks in this same chunk.
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
        if (section == null)
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
        if (section == null)
        {
            return;
        }

        var blockPos = new Vector3i(localX + GridX * 16, worldY, localZ + GridZ * 16);

        section.RemoveBlockAt(localX, worldY & 15, localZ);
        TickableBlocks.Remove(blockPos);
        LightSourceBlocks.Remove(blockPos);

        // Recomputed after the removal, otherwise the block being removed is still found as the top one.
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

        // Assigned rather than added: the slot may already hold the state of the block being replaced.
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

    /// <summary>
    /// The height of the lowest section that is see through, counting down from the top while every section
    /// above is also see through. Sunlight can fill everything above it without any propagation work.
    /// </summary>
    public uint GetLowestEmptySectionAfterEachOtherFromTop()
    {
        uint lowestSection = Constants.NUM_SECTIONS_IN_CHUNKS - 1;

        for (int i = Constants.NUM_SECTIONS_IN_CHUNKS - 1; i >= 0; i--)
        {
            Section? section = Sections[i];
            if (section != null && !section.IsFullTransparent)
            {
                break;
            }

            lowestSection = (uint)i;
        }

        return lowestSection;
    }

    /// <summary>The number of bytes this chunk takes on the wire.</summary>
    public int GetPayloadSize()
    {
        int size = 0;

        for (int i = 0; i < Constants.NUM_SECTIONS_IN_CHUNKS; i++)
        {
            // One byte flags whether the section has any blocks at all.
            size++;

            Section? section = Sections[i];
            if (section == null)
            {
                continue;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        // Every slot carries its block id, plus whatever the state adds on top.
                        size += sizeof(ushort);
                        size += section.GetBlockAt(x, y, z)?.PayloadSize() ?? 0;
                    }
                }
            }
        }

        return size;
    }
}
