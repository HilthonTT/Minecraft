using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A box inside one block cell, in block-local coordinates from 0 to 1.
/// </summary>
public readonly record struct Box(Vector3 Min, Vector3 Max)
{
    public static Box FromSixteenths(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) =>
        new(new Vector3(minX, minY, minZ) / 16F, new Vector3(maxX, maxY, maxZ) / 16F);

    /// <summary>
    /// Turns the box about the vertical axis through the centre of the cell, so that a shape drawn facing
    /// <see cref="Direction.Front"/> faces <paramref name="facing"/> instead.
    /// </summary>
    public Box RotatedToFace(Direction facing)
    {
        int quarterTurns = facing switch
        {
            Direction.Front => 0,
            Direction.Left => 1,
            Direction.Back => 2,
            Direction.Right => 3,
            _ => 0,
        };

        Vector3 min = Min;
        Vector3 max = Max;
        for (int turn = 0; turn < quarterTurns; turn++)
        {
            // A quarter turn takes +Z (front) to -X (left): (x, z) becomes (1 - z, x).
            (min, max) = (new Vector3(1 - max.Z, min.Y, min.X), new Vector3(1 - min.Z, max.Y, max.X));
        }

        return new Box(min, max);
    }
}

public static class BoxFaces
{
    /// <summary>
    /// Builds the six faces of each box with the same corner order and texture orientation as a full block, so a
    /// box cut from a cell shows the part of the texture that the same region of a whole block would. That keeps
    /// the grain of planks and the courses of cobblestone lined up between a slab and the block beside it.
    /// </summary>
    public static BlockFace[] Build(TextureAtlas atlas, IEnumerable<Box> boxes, Func<Direction, Vector2> cellFor)
    {
        List<BlockFace> faces = [];

        foreach (Box box in boxes)
        {
            foreach (Direction direction in Enum.GetValues<Direction>())
            {
                faces.Add(BuildFace(atlas, box, direction, cellFor(direction)));
            }
        }

        return [.. faces];
    }

    private static BlockFace BuildFace(TextureAtlas atlas, Box box, Direction direction, Vector2 cell)
    {
        float x0 = box.Min.X, y0 = box.Min.Y, z0 = box.Min.Z;
        float x1 = box.Max.X, y1 = box.Max.Y, z1 = box.Max.Z;

        Vector3[] positions = direction switch
        {
            Direction.Back => [new(x1, y0, z0), new(x0, y0, z0), new(x0, y1, z0), new(x1, y1, z0)],
            Direction.Right => [new(x1, y0, z1), new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1)],
            Direction.Front => [new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1)],
            Direction.Left => [new(x0, y0, z0), new(x0, y0, z1), new(x0, y1, z1), new(x0, y1, z0)],
            Direction.Top => [new(x0, y1, z0), new(x0, y1, z1), new(x1, y1, z1), new(x1, y1, z0)],
            Direction.Bottom => [new(x1, y0, z0), new(x1, y0, z1), new(x0, y0, z1), new(x0, y0, z0)],
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        var uvs = new Vector2[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            (float u, float v) = TextureFractionAt(positions[i], direction);
            uvs[i] = atlas.GetTextureCoord(cell, u, v);
        }

        return new BlockFace(positions, uvs);
    }

    // These are the texture mappings FullBlockModel's faces and TextureAtlas.GetTextureCoords produce between them,
    // written per vertex so they hold for a face that covers only part of the cell.
    private static (float U, float V) TextureFractionAt(Vector3 point, Direction direction) => direction switch
    {
        Direction.Back => (point.X, 1 - point.Y),
        Direction.Right => (point.Z, 1 - point.Y),
        Direction.Front => (1 - point.X, 1 - point.Y),
        Direction.Left => (1 - point.Z, 1 - point.Y),
        Direction.Top => (1 - point.Z, 1 - point.X),
        Direction.Bottom => (1 - point.Z, point.X),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}
