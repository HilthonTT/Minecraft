using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Structures.Villages;

public sealed class VillageHouse : VillageBuilding
{
    private const int RoofOverhang = 1;

    private const int WindowHeight = 2;

    private readonly StructureBounds _walls;
    private readonly int _floorY;
    private readonly int _wallHeight;
    private readonly Direction _facing;
    private readonly int _doorX;
    private readonly int _doorZ;

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
                    writer.ClearColumn(worldX, worldZ, _floorY + 1, _floorY + Math.Min(2, _wallHeight));
                }
                else if (IsWindow(worldX, worldZ) && _wallHeight >= WindowHeight)
                {
                    writer.Clear(worldX, _floorY + WindowHeight, worldZ);
                }
            }
        }
    }

    private bool IsDoorFrame(int worldX, int worldZ)
    {
        if (_facing is Direction.Back or Direction.Front)
        {
            return worldZ == _doorZ && Math.Abs(worldX - _doorX) == 1;
        }

        return worldX == _doorX && Math.Abs(worldZ - _doorZ) == 1;
    }

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
