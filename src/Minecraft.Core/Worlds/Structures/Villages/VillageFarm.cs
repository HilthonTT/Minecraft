using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures.Villages;

public sealed class VillageFarm : VillageBuilding
{
    private const int RowsPerWalkway = 3;

    private readonly StructureBounds _plot;
    private readonly int _floorY;
    private readonly int _gateX;
    private readonly int _gateZ;

    public VillageFarm(StructureBounds plot, int floorY, Direction facing)
        : base(plot, facing, 1)
    {
        _plot = plot;
        _floorY = floorY;
        (_gateX, _gateZ) = Door;
    }

    public override StructureBounds Bounds => _plot;

    public override void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain)
    {
        LevelPlot(writer, terrain, _plot, _floorY, palette.Foundation, 5);

        for (int worldX = _plot.MinX; worldX <= _plot.MaxX; worldX++)
        {
            for (int worldZ = _plot.MinZ; worldZ <= _plot.MaxZ; worldZ++)
            {
                bool onEdge = worldX == _plot.MinX || worldX == _plot.MaxX
                              || worldZ == _plot.MinZ || worldZ == _plot.MaxZ;

                if (onEdge)
                {
                    writer.SetBlock(worldX, _floorY, worldZ, palette.Path);

                    if (worldX != _gateX || worldZ != _gateZ)
                    {
                        writer.SetBlock(worldX, _floorY + 1, worldZ, palette.Fence);
                    }

                    continue;
                }

                if ((worldX - _plot.MinX) % RowsPerWalkway == 0)
                {
                    writer.SetBlock(worldX, _floorY, worldZ, palette.Path);
                    continue;
                }

                writer.SetBlock(worldX, _floorY, worldZ, palette.FarmSoil);
                writer.SetBlock(worldX, _floorY + 1, worldZ, BlockRegistry.Wheat);
            }
        }
    }
}
