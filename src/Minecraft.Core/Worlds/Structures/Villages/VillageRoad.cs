namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// A road from a doorstep back to the well, bent once so that it runs along the axes rather than diagonally.
/// <para>
/// A road follows the ground instead of levelling it, so it climbs whatever it crosses. It is laid before the
/// buildings are, which lets a building pave over the stretch running under it rather than the other way
/// round.
/// </para>
/// </summary>
public sealed class VillageRoad : VillagePiece
{
    /// <summary>How much open space is cleared above the road, enough to take out grass and undergrowth.</summary>
    private const int Clearance = 2;

    private readonly (int X, int Z)[] _tiles;
    private readonly StructureBounds _bounds;

    private VillageRoad((int X, int Z)[] tiles, StructureBounds bounds)
    {
        _tiles = tiles;
        _bounds = bounds;
    }

    public override StructureBounds Bounds => _bounds;

    /// <summary>
    /// Lays a road between two points, running along x first and then along z.
    /// </summary>
    public static VillageRoad Between((int X, int Z) from, (int X, int Z) to)
    {
        var tiles = new List<(int X, int Z)>();

        int stepX = Math.Sign(to.X - from.X);
        for (int x = from.X; x != to.X; x += stepX)
        {
            tiles.Add((x, from.Z));
        }

        int stepZ = Math.Sign(to.Z - from.Z);
        for (int z = from.Z; z != to.Z; z += stepZ)
        {
            tiles.Add((to.X, z));
        }

        tiles.Add(to);

        var bounds = new StructureBounds(
            Math.Min(from.X, to.X),
            Math.Min(from.Z, to.Z),
            Math.Max(from.X, to.X),
            Math.Max(from.Z, to.Z));

        return new VillageRoad([.. tiles], bounds);
    }

    public override void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain)
    {
        foreach ((int X, int Z) tile in _tiles)
        {
            if (!writer.Bounds.Contains(tile.X, tile.Z))
            {
                continue;
            }

            int surfaceY = terrain.SampleColumn(tile.X, tile.Z).SurfaceY;
            writer.SetBlock(tile.X, surfaceY, tile.Z, palette.Path);
            writer.ClearColumn(tile.X, tile.Z, surfaceY + 1, surfaceY + Clearance);
        }
    }
}
