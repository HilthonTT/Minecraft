using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Structures.Villages;

public sealed class Village : IStructure
{
    private const int PlotCellSize = 12;

    private const int PlotGridRadius = 2;

    public const int MaxRadiusInBlocks = (PlotGridRadius * PlotCellSize) + PlotCellSize;

    private const int PlotJitter = 2;

    private const int BuildingChancePercent = 55;
    private const int FarmChancePercent = 30;
    private const int MaxBuildings = 10;

    private const int MinBuildings = 4;

    private const int MaxPlotHeightSpread = 4;

    private const int MaxSiteHeightSpread = 26;

    private const int SiteSampleStep = 8;

    private const double MaxUnsettleableSiteShare = 0.25D;

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
                bool wanted = random.Next(100) < BuildingChancePercent;
                bool isFarm = random.Next(100) < FarmChancePercent;
                int plotCenterX = centerX + (cellX * PlotCellSize) + random.Next(-PlotJitter, PlotJitter + 1);
                int plotCenterZ = centerZ + (cellZ * PlotCellSize) + random.Next(-PlotJitter, PlotJitter + 1);
                int width = 5 + (random.Next(2) * 2);
                int depth = 5 + (random.Next(2) * 2);
                int wallHeight = 3 + random.Next(2);

                if ((cellX == 0 && cellZ == 0) || !wanted || buildings.Count >= MaxBuildings)
                {
                    continue;
                }

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
                if (column.Biome.SettlementPalette is null || column.SurfaceY <= terrain.SeaLevel)
                {
                    unsettleable++;
                }

                lowest = Math.Min(lowest, column.SurfaceY);
                highest = Math.Max(highest, column.SurfaceY);
            }
        }

        return unsettleable <= samples * MaxUnsettleableSiteShare && highest - lowest <= MaxSiteHeightSpread;
    }

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

        if (floorY <= terrain.SeaLevel)
        {
            return false;
        }

        return floorY > BuildHeightMargin && floorY < Constants.MAX_BUILD_HEIGHT - BuildHeightMargin;
    }

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
