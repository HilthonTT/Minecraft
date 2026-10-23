using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public enum TreeKind
{
    Oak,
    Birch,
    Spruce,
}

/// <summary>
/// The tree a sapling grows into. The generators that scatter trees across new terrain write straight into a chunk
/// that nothing is looking at yet; a sapling grows in a live world, possibly across a chunk border and in front of
/// a player, so its tree is laid out here as a list of blocks for the world to place in the ordinary way.
/// </summary>
public static class SaplingTree
{
    public static Block LogFor(TreeKind kind) => kind switch
    {
        TreeKind.Birch => BlockRegistry.BirchLog,
        TreeKind.Spruce => BlockRegistry.SpruceLog,
        _ => BlockRegistry.OakLog,
    };

    /// <summary>Offsets from the sapling, trunk first and then canopy.</summary>
    public static List<(Vector3i Offset, Block Block)> Layout(TreeKind kind, Random random)
    {
        int trunkHeight = kind switch
        {
            TreeKind.Birch => 5 + random.Next(3),
            TreeKind.Spruce => 6 + random.Next(3),
            _ => 4 + random.Next(3),
        };

        Block log = LogFor(kind);
        List<(Vector3i, Block)> layout = [];

        for (int y = 0; y < trunkHeight; y++)
        {
            layout.Add((new Vector3i(0, y, 0), log));
        }

        if (kind == TreeKind.Spruce)
        {
            AddSpruceCanopy(layout, trunkHeight);
        }
        else
        {
            AddRoundCanopy(layout, trunkHeight, random);
        }

        return layout;
    }

    // Two wide layers around the top of the trunk and two narrow ones over it.
    private static void AddRoundCanopy(List<(Vector3i, Block)> layout, int trunkHeight, Random random)
    {
        for (int y = trunkHeight - 3; y <= trunkHeight; y++)
        {
            int radius = y >= trunkHeight - 1 ? 1 : 2;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    bool corner = Math.Abs(dx) == radius && Math.Abs(dz) == radius;
                    bool trunk = dx == 0 && dz == 0 && y < trunkHeight;

                    if (trunk || (corner && (radius == 1 || random.Next(2) == 0)))
                    {
                        continue;
                    }

                    layout.Add((new Vector3i(dx, y, dz), BlockRegistry.OakLeaves));
                }
            }
        }
    }

    // A cone of rings that narrows and widens again on the way up, to a single block on the tip.
    private static void AddSpruceCanopy(List<(Vector3i, Block)> layout, int trunkHeight)
    {
        layout.Add((new Vector3i(0, trunkHeight, 0), BlockRegistry.OakLeaves));

        for (int y = trunkHeight - 1; y >= 2; y--)
        {
            int fromTop = trunkHeight - y;
            int radius = fromTop == 1 ? 1 : fromTop % 2 == 0 ? 1 : 2;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    bool corner = Math.Abs(dx) == radius && Math.Abs(dz) == radius;
                    if ((dx == 0 && dz == 0) || corner)
                    {
                        continue;
                    }

                    layout.Add((new Vector3i(dx, y, dz), BlockRegistry.OakLeaves));
                }
            }
        }
    }
}
