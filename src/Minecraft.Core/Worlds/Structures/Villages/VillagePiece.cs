using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures.Villages;

public abstract class VillagePiece
{
    private const int FoundationDepth = 2;

    public abstract StructureBounds Bounds { get; }

    public abstract void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain);

    protected static void LevelPlot(
        StructureWriter writer,
        ITerrainSampler terrain,
        StructureBounds plot,
        int floorY,
        Block foundation,
        int clearance)
    {
        for (int worldX = plot.MinX; worldX <= plot.MaxX; worldX++)
        {
            for (int worldZ = plot.MinZ; worldZ <= plot.MaxZ; worldZ++)
            {
                if (!writer.Bounds.Contains(worldX, worldZ))
                {
                    continue;
                }

                int surfaceY = terrain.SampleColumn(worldX, worldZ).SurfaceY;

                writer.ClearColumn(worldX, worldZ, floorY + 1, Math.Max(surfaceY, floorY) + clearance);

                writer.FillColumn(
                    worldX,
                    worldZ,
                    Math.Max(1, Math.Min(surfaceY, floorY) - FoundationDepth),
                    floorY,
                    foundation);
            }
        }
    }

    protected static void FillLayer(StructureWriter writer, StructureBounds area, int worldY, Block block)
    {
        for (int worldX = area.MinX; worldX <= area.MaxX; worldX++)
        {
            for (int worldZ = area.MinZ; worldZ <= area.MaxZ; worldZ++)
            {
                writer.SetBlock(worldX, worldY, worldZ, block);
            }
        }
    }
}
