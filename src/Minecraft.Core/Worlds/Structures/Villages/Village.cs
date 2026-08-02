using Minecraft.Core.Utilities;

namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// A cluster of houses and farms laid out around a central well, joined to it by roads.
/// <para>
/// Plots are drawn from a coarse grid of cells around the well and jittered inside their cell, which spaces
/// the buildings out without lining them up. A plot whose ground is too uneven is simply left empty, so a
/// village thins out as it runs into a hillside instead of terracing its way up one.
/// </para>
/// </summary>
public sealed class Village : IStructure
{
    /// <summary>The size of one plot cell. Buildings are placed at most one per cell.</summary>
    private const int PlotCellSize = 12;

    /// <summary>How many cells the village reaches out from the well in each direction.</summary>
    private const int PlotGridRadius = 2;

    /// <summary>
    /// The furthest a village can possibly reach from its centre. Nothing is placed anywhere near this far
    /// out, but it bounds how far away a chunk can still be reached into by one.
    /// </summary>
    public const int MaxRadiusInBlocks = (PlotGridRadius * PlotCellSize) + PlotCellSize;

    /// <summary>How far a plot is nudged out of the middle of its cell.</summary>
    private const int PlotJitter = 2;

    private const int BuildingChancePercent = 55;
    private const int FarmChancePercent = 30;
    private const int MaxBuildings = 10;

    /// <summary>
    /// Below this a settlement is not worth calling a village. Rolling ground rejects plot after plot, and
    /// without a floor the world would fill up with lone houses on the one flat spot of a hillside.
    /// </summary>
    private const int MinBuildings = 4;

    /// <summary>How much higher one corner of a plot may sit than another before it is abandoned.</summary>
    private const int MaxPlotHeightSpread = 4;

    /// <summary>
    /// The same for the village as a whole, judged before any of it is laid out. Deliberately loose: it is
    /// only there to turn down mountainsides cheaply, and the plots themselves are what decide how much of a
    /// rolling site actually gets built on.
    /// </summary>
    private const int MaxSiteHeightSpread = 26;

    /// <summary>How far apart the columns are that the site is judged on.</summary>
    private const int SiteSampleStep = 8;

    /// <summary>
    /// The share of a site that may fall in a biome nobody settles before the site is given up on. A village
    /// is allowed to back onto a mountain, and the plots that would sit on it are turned down one at a time.
    /// </summary>
    private const double MaxUnsettleableSiteShare = 0.25D;

    /// <summary>Room left above and below for a building, so nothing has to be clipped to the build height.</summary>
    private const int BuildHeightMargin = 16;

    private readonly VillagePiece[] _pieces;
    private readonly StructurePalette _palette;
    private readonly ITerrainSampler _terrain;

    private Village(VillagePiece[] pieces, StructurePalette palette, ITerrainSampler terrain)
    {
        _pieces = pieces;
        _palette = palette;
        _terrain = terrain;

        StructureBounds bounds = pieces[0].Bounds;
        foreach (VillagePiece piece in pieces)
        {
            StructureBounds pieceBounds = piece.Bounds;
            bounds = new StructureBounds(
                Math.Min(bounds.MinX, pieceBounds.MinX),
                Math.Min(bounds.MinZ, pieceBounds.MinZ),
                Math.Max(bounds.MaxX, pieceBounds.MaxX),
                Math.Max(bounds.MaxZ, pieceBounds.MaxZ));
        }

        Bounds = bounds;
    }

    public StructureBounds Bounds { get; }

    /// <summary>
    /// Lays out the village that stands at the given position, or returns null when nothing can stand there.
    /// </summary>
    /// <param name="seed">
    /// Derived from the world seed and this position. The layout follows from it alone, so every chunk the
    /// village covers works out the same village.
    /// </param>
    public static Village? TryCreate(int seed, int centerX, int centerZ, ITerrainSampler terrain)
    {
        TerrainColumn center = terrain.SampleColumn(centerX, centerZ);
        if (center.Biome.SettlementPalette is not StructurePalette palette)
        {
            return null;
        }

        if (!IsSiteSuitable(terrain, centerX, centerZ))
        {
            return null;
        }

        var random = new Random(seed);
        var roads = new List<VillagePiece>();
        var buildings = new List<VillageBuilding>();
        var taken = new List<StructureBounds>();

        var well = new VillageWell(centerX, centerZ, center.SurfaceY);
        taken.Add(well.Bounds);

        for (int cellX = -PlotGridRadius; cellX <= PlotGridRadius; cellX++)
        {
            for (int cellZ = -PlotGridRadius; cellZ <= PlotGridRadius; cellZ++)
            {
                // Drawn before anything else is decided so that the sequence of numbers a village consumes
                // does not depend on which plots happened to work out.
                bool wanted = random.Next(100) < BuildingChancePercent;
                bool isFarm = random.Next(100) < FarmChancePercent;
                int plotCenterX = centerX + (cellX * PlotCellSize) + random.Next(-PlotJitter, PlotJitter + 1);
                int plotCenterZ = centerZ + (cellZ * PlotCellSize) + random.Next(-PlotJitter, PlotJitter + 1);
                int width = 5 + (random.Next(2) * 2);
                int depth = 5 + (random.Next(2) * 2);
                int wallHeight = 3 + random.Next(2);

                // The well sits in the middle cell, and a village only grows so large however many cells
                // happen to be free.
                if ((cellX == 0 && cellZ == 0) || !wanted || buildings.Count >= MaxBuildings)
                {
                    continue;
                }

                // Judged on the plot itself rather than on the village, so that a village backing onto a
                // mountain simply stops where the mountain starts.
                if (terrain.SampleColumn(plotCenterX, plotCenterZ).Biome.SettlementPalette is null)
                {
                    continue;
                }

                StructureBounds plot = StructureBounds.FromCenter(plotCenterX, plotCenterZ, width, depth);
                if (!TryFindFloorHeight(terrain, plot, out int floorY))
                {
                    continue;
                }

                Direction facing = FacingTowards(plotCenterX, plotCenterZ, centerX, centerZ);
                VillageBuilding building = isFarm
                    ? new VillageFarm(plot, floorY, facing)
                    : new VillageHouse(plot, floorY, wallHeight, facing);

                // Kept a block apart so that two plots never share a wall or a plinth.
                StructureBounds footprint = building.Bounds.Expand(1);
                if (taken.Exists(footprint.Intersects))
                {
                    continue;
                }

                taken.Add(footprint);
                buildings.Add(building);
                roads.Add(VillageRoad.Between(building.Doorstep, (centerX, centerZ)));
            }
        }

        if (buildings.Count < MinBuildings)
        {
            return null;
        }

        // Roads first, so that the well's courtyard and then the buildings pave over the stretches running
        // under them rather than the other way round.
        VillagePiece[] pieces = [.. roads, well, .. buildings];
        return new Village(pieces, palette, terrain);
    }

    public void PlaceInto(StructureWriter writer)
    {
        foreach (VillagePiece piece in _pieces)
        {
            if (!piece.Bounds.IntersectsChunk(writer.ChunkX, writer.ChunkZ))
            {
                continue;
            }

            piece.Build(writer, _palette, _terrain);
        }
    }

    /// <summary>
    /// Whether the ground over the whole village is level enough and settled enough to build on. Judged on a
    /// coarse grid, which is enough to turn down a mountainside without sampling every column of it.
    /// </summary>
    private static bool IsSiteSuitable(ITerrainSampler terrain, int centerX, int centerZ)
    {
        const int reach = PlotGridRadius * PlotCellSize;

        int lowest = int.MaxValue;
        int highest = int.MinValue;
        int samples = 0;
        int unsettleable = 0;

        for (int offsetX = -reach; offsetX <= reach; offsetX += SiteSampleStep)
        {
            for (int offsetZ = -reach; offsetZ <= reach; offsetZ += SiteSampleStep)
            {
                TerrainColumn column = terrain.SampleColumn(centerX + offsetX, centerZ + offsetZ);

                samples++;
                if (column.Biome.SettlementPalette is null)
                {
                    unsettleable++;
                }

                lowest = Math.Min(lowest, column.SurfaceY);
                highest = Math.Max(highest, column.SurfaceY);
            }
        }

        return unsettleable <= samples * MaxUnsettleableSiteShare && highest - lowest <= MaxSiteHeightSpread;
    }

    /// <summary>
    /// The height a plot would be levelled to, or false when its ground is too uneven to level at all.
    /// </summary>
    private static bool TryFindFloorHeight(ITerrainSampler terrain, StructureBounds plot, out int floorY)
    {
        floorY = 0;

        int lowest = int.MaxValue;
        int highest = int.MinValue;
        int total = 0;

        for (int worldX = plot.MinX; worldX <= plot.MaxX; worldX++)
        {
            for (int worldZ = plot.MinZ; worldZ <= plot.MaxZ; worldZ++)
            {
                int surfaceY = terrain.SampleColumn(worldX, worldZ).SurfaceY;
                lowest = Math.Min(lowest, surfaceY);
                highest = Math.Max(highest, surfaceY);
                total += surfaceY;
            }
        }

        if (highest - lowest > MaxPlotHeightSpread)
        {
            return false;
        }

        floorY = total / (plot.Width * plot.Depth);
        return floorY > BuildHeightMargin && floorY < Constants.MAX_BUILD_HEIGHT - BuildHeightMargin;
    }

    /// <summary>The side of a plot that is turned towards the village centre.</summary>
    private static Direction FacingTowards(int fromX, int fromZ, int toX, int toZ)
    {
        int deltaX = toX - fromX;
        int deltaZ = toZ - fromZ;

        if (Math.Abs(deltaX) >= Math.Abs(deltaZ))
        {
            return deltaX >= 0 ? Direction.Right : Direction.Left;
        }

        return deltaZ >= 0 ? Direction.Front : Direction.Back;
    }
}
