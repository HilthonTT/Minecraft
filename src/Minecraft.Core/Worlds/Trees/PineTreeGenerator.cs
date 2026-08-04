using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Trees;

/// <summary>
/// A pine: a dark trunk under a spire of leaves that steps in as it climbs, and that starts low enough down
/// the trunk to give the tree a skirt.
/// </summary>
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

        // The skirt starts a couple of blocks off the ground and the spire tapers from there. Each pair of
        // layers steps in by one and then flares back out, which is what gives a conifer its tiered look
        // instead of a smooth cone.
        int skirtBase = worldY + 2;
        int layers = trunkHeight - 2;

        for (int layer = 0; layer < layers; layer++)
        {
            // Counted down from the top so the taper holds however tall this particular tree came out.
            int fromTop = layers - 1 - layer;
            int radius = fromTop switch
            {
                0 => 0,
                1 => 1,
                _ => 1 + ((fromTop + 1) % 2),
            };

            PlaceLeafDisc(chunk, leaves, localX, skirtBase + layer, localZ, radius, cutCorners: radius > 1);
        }

        // A single block closing the top of the trunk off.
        PlaceLeafDisc(chunk, leaves, localX, worldY + trunkHeight, localZ, radius: 0, cutCorners: false);
    }
}
