namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// The well at the heart of a village: a paved courtyard around a walled shaft, roofed over on four posts.
/// Every road in the village runs back to it.
/// </summary>
public sealed class VillageWell : VillagePiece
{
    /// <summary>How far the paved courtyard reaches out from the shaft.</summary>
    private const int CourtyardRadius = 2;

    /// <summary>How deep the shaft is sunk below the courtyard.</summary>
    private const int ShaftDepth = 4;

    /// <summary>How high the posts hold the roof above the wall around the shaft.</summary>
    private const int PostHeight = 2;

    private readonly int _centerX;
    private readonly int _centerZ;
    private readonly int _floorY;

    public VillageWell(int centerX, int centerZ, int floorY)
    {
        _centerX = centerX;
        _centerZ = centerZ;
        _floorY = floorY;
    }

    public override StructureBounds Bounds =>
        StructureBounds.FromCenter(_centerX, _centerZ, (CourtyardRadius * 2) + 1, (CourtyardRadius * 2) + 1);

    public override void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain)
    {
        LevelPlot(writer, terrain, Bounds, _floorY, palette.Foundation, PostHeight + 4);
        FillLayer(writer, Bounds, _floorY, palette.Path);

        var shaft = StructureBounds.FromCenter(_centerX, _centerZ, 3, 3);
        int rimY = _floorY + 1;
        int roofY = rimY + PostHeight + 1;

        for (int worldX = shaft.MinX; worldX <= shaft.MaxX; worldX++)
        {
            for (int worldZ = shaft.MinZ; worldZ <= shaft.MaxZ; worldZ++)
            {
                bool isCorner = (worldX == shaft.MinX || worldX == shaft.MaxX)
                                && (worldZ == shaft.MinZ || worldZ == shaft.MaxZ);

                if (worldX == _centerX && worldZ == _centerZ)
                {
                    writer.ClearColumn(worldX, worldZ, _floorY - ShaftDepth, roofY - 1);
                    continue;
                }

                writer.SetBlock(worldX, rimY, worldZ, palette.Foundation);

                if (isCorner)
                {
                    for (int height = 1; height <= PostHeight; height++)
                    {
                        writer.SetBlock(worldX, rimY + height, worldZ, palette.Corner);
                    }
                }
            }
        }

        FillLayer(writer, shaft, roofY, palette.Roof);
    }
}
