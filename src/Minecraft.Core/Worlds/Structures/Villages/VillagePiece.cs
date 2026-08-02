using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// One thing a village is made of: a house, a farm, the well or a stretch of road.
/// <para>
/// A piece decides everything about itself while the village is being laid out and holds no randomness of its
/// own, so building it is the same work no matter which of the chunks it covers is asking.
/// </para>
/// </summary>
public abstract class VillagePiece
{
    /// <summary>How far below the lowest ground of a plot its foundation reaches.</summary>
    private const int FoundationDepth = 2;

    /// <summary>Everything this piece occupies, which decides the chunks it has to be built into.</summary>
    public abstract StructureBounds Bounds { get; }

    /// <summary>Writes whatever part of this piece falls inside the writer's chunk.</summary>
    public abstract void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain);

    /// <summary>
    /// Cuts a plot into the hillside: everything above the floor is taken away and everything below it is
    /// filled in, so that a building standing on the plot is neither buried nor left on stilts.
    /// </summary>
    /// <param name="clearance">How much open space is left above the floor.</param>
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
                // Columns of the plot that belong to another chunk are that chunk's job, and sampling the
                // terrain for them here would only be thrown away.
                if (!writer.Bounds.Contains(worldX, worldZ))
                {
                    continue;
                }

                int surfaceY = terrain.SampleColumn(worldX, worldZ).SurfaceY;

                // Cleared up to the clearance above whichever is higher, the floor or the hillside, so that a
                // plot cut into a slope has the ground behind it taken away too.
                writer.ClearColumn(worldX, worldZ, floorY + 1, Math.Max(surfaceY, floorY) + clearance);

                // Filled through the floor rather than up to it, so that the parts of the plot the caller
                // does not pave over are left as a plinth instead of a trench.
                writer.FillColumn(
                    worldX,
                    worldZ,
                    Math.Max(1, Math.Min(surfaceY, floorY) - FoundationDepth),
                    floorY,
                    foundation);
            }
        }
    }

    /// <summary>Fills one horizontal layer of a box.</summary>
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
