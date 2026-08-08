using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

/// <summary>
/// A birch: a tall pale trunk carrying a crown that sits entirely at the top of it, so a birch wood is open
/// underneath in a way an oak wood is not.
/// </summary>
public sealed class BirchTreeGenerator : TreeGenerator
{
    private const int MinTrunkHeight = 6;
    private const int MaxTrunkHeight = 9;

    protected override int CanopyRadius => 2;

    protected override int MaxHeight => MaxTrunkHeight + 2;

    protected override void Grow(Chunk chunk, int localX, int worldY, int localZ, Random random)
    {
        BlockState log = BlockRegistry.GetState(BlockRegistry.BirchLog);
        BlockState leaves = BlockRegistry.GetState(BlockRegistry.OakLeaves);

        int trunkHeight = random.Next(MinTrunkHeight, MaxTrunkHeight + 1);
        PlaceTrunk(chunk, log, localX, worldY, localZ, trunkHeight);

        // Two wide layers around the last of the bare trunk, then a narrow cap over the top of it.
        int crownBase = worldY + trunkHeight - 3;
        PlaceLeafDisc(chunk, leaves, localX, crownBase, localZ, radius: 2, cutCorners: true);
        PlaceLeafDisc(chunk, leaves, localX, crownBase + 1, localZ, radius: 2, cutCorners: true);
        PlaceLeafDisc(chunk, leaves, localX, crownBase + 2, localZ, radius: 1, cutCorners: false);
        PlaceLeafDisc(chunk, leaves, localX, crownBase + 3, localZ, radius: 1, cutCorners: true);
    }
}
