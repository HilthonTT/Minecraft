using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public sealed class PineTreeGenerator : TreeGenerator
{
    private const int MinTrunkHeight = 7;
    private const int MaxTrunkHeight = 12;

    protected override int CanopyRadius => 2;

    protected override int MaxHeight => MaxTrunkHeight + 2;

    protected override void Grow(Chunk chunk, int localX, int worldY, int localZ, Random random)
    {
        BlockState log = BlockRegistry.GetState(BlockRegistry.SpruceLog);
        BlockState leaves = BlockRegistry.GetState(BlockRegistry.OakLeaves);

        int trunkHeight = random.Next(MinTrunkHeight, MaxTrunkHeight + 1);
        PlaceTrunk(chunk, log, localX, worldY, localZ, trunkHeight);

        int skirtBase = worldY + 2;
        int layers = trunkHeight - 2;

        for (int layer = 0; layer < layers; layer++)
        {
            int fromTop = layers - 1 - layer;
            int radius = fromTop switch
            {
                0 => 0,
                1 => 1,
                _ => 1 + ((fromTop + 1) % 2),
            };

            PlaceLeafDisc(chunk, leaves, localX, skirtBase + layer, localZ, radius, cutCorners: radius > 1);
        }

        PlaceLeafDisc(chunk, leaves, localX, worldY + trunkHeight, localZ, radius: 0, cutCorners: false);
    }
}
