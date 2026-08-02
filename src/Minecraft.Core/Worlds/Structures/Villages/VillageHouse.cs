using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// A one room house: a levelled plot with a plinth around it, four walls with posted corners, a doorway
/// facing the village and a roof of two stepped layers.
/// </summary>
public sealed class VillageHouse : VillageBuilding
{
    /// <summary>How far the lower roof layer hangs over the walls.</summary>
    private const int RoofOverhang = 1;

    /// <summary>How high up the wall windows are cut.</summary>
    private const int WindowHeight = 2;

    private readonly StructureBounds _walls;
    private readonly int _floorY;
    private readonly int _wallHeight;
    private readonly Direction _facing;
    private readonly int _doorX;
    private readonly int _doorZ;

    /// <param name="walls">The outer face of the walls, which is also the plot that gets levelled.</param>
    /// <param name="facing">The side the door is on, which is the one turned towards the village centre.</param>
    public VillageHouse(StructureBounds walls, int floorY, int wallHeight, Direction facing)
        : base(walls, facing, RoofOverhang + 1)
    {
        _walls = walls;
        _floorY = floorY;
        _wallHeight = wallHeight;
        _facing = facing;
        (_doorX, _doorZ) = Door;
    }

    public override StructureBounds Bounds => _walls.Expand(RoofOverhang);

    public override void Build(StructureWriter writer, StructurePalette palette, ITerrainSampler terrain)
    {
        int roofY = _floorY + _wallHeight + 1;

        LevelPlot(writer, terrain, Bounds, _floorY, palette.Foundation, _wallHeight + 4);

        FillLayer(writer, _walls, _floorY, palette.Floor);
        BuildWalls(writer, palette);

        FillLayer(writer, Bounds, roofY, palette.Roof);

        // The upper layer is inset on all sides, which reads as a pitch without needing a slope. Houses too
        // small to inset keep the single flat roof.
        if (_walls.Width >= 5 && _walls.Depth >= 5)
        {
            var upperRoof = new StructureBounds(_walls.MinX + 1, _walls.MinZ + 1, _walls.MaxX - 1, _walls.MaxZ - 1);
            FillLayer(writer, upperRoof, roofY + 1, palette.Roof);
        }
    }

    private void BuildWalls(StructureWriter writer, StructurePalette palette)
    {
        for (int worldX = _walls.MinX; worldX <= _walls.MaxX; worldX++)
        {
            for (int worldZ = _walls.MinZ; worldZ <= _walls.MaxZ; worldZ++)
            {
                bool onXEdge = worldX == _walls.MinX || worldX == _walls.MaxX;
                bool onZEdge = worldZ == _walls.MinZ || worldZ == _walls.MaxZ;
                if (!onXEdge && !onZEdge)
                {
                    continue;
                }

                bool isPost = (onXEdge && onZEdge) || IsDoorFrame(worldX, worldZ);
                Block block = isPost ? palette.Corner : palette.Wall;

                for (int height = 1; height <= _wallHeight; height++)
                {
                    writer.SetBlock(worldX, _floorY + height, worldZ, block);
                }

                if (worldX == _doorX && worldZ == _doorZ)
                {
                    // Cut after the wall went up rather than skipped, so the doorway is the same hole however
                    // tall the wall around it is.
                    writer.ClearColumn(worldX, worldZ, _floorY + 1, _floorY + Math.Min(2, _wallHeight));
                }
                else if (IsWindow(worldX, worldZ) && _wallHeight >= WindowHeight)
                {
                    writer.Clear(worldX, _floorY + WindowHeight, worldZ);
                }
            }
        }
    }

    /// <summary>Whether a wall column is one of the two flanking the door.</summary>
    private bool IsDoorFrame(int worldX, int worldZ)
    {
        if (_facing is Direction.Back or Direction.Front)
        {
            return worldZ == _doorZ && Math.Abs(worldX - _doorX) == 1;
        }

        return worldX == _doorX && Math.Abs(worldZ - _doorZ) == 1;
    }

    /// <summary>
    /// Whether a wall column is cut out as a window. They are spaced two apart along each wall and kept two
    /// blocks clear of the corners, so a wall shorter than five blocks ends up blank.
    /// </summary>
    private bool IsWindow(int worldX, int worldZ)
    {
        if (IsDoorFrame(worldX, worldZ) || (worldX == _doorX && worldZ == _doorZ))
        {
            return false;
        }

        bool onXEdge = worldX == _walls.MinX || worldX == _walls.MaxX;
        if (onXEdge && worldZ >= _walls.MinZ + 2 && worldZ <= _walls.MaxZ - 2)
        {
            return (worldZ - _walls.MinZ) % 2 == 0;
        }

        bool onZEdge = worldZ == _walls.MinZ || worldZ == _walls.MaxZ;
        if (onZEdge && worldX >= _walls.MinX + 2 && worldX <= _walls.MaxX - 2)
        {
            return (worldX - _walls.MinX) % 2 == 0;
        }

        return false;
    }
}
