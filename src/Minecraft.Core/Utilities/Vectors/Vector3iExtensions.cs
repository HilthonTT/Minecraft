using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Vectors;

/// <summary>
/// Block grid helpers for <see cref="Vector3i"/>. The engine treats a <see cref="Vector3i"/> as a block
/// position, either in world space or local to a chunk, so the neighbour and chunk local helpers live here
/// rather than on a hand rolled vector type.
/// </summary>
public static class Vector3iExtensions
{
    public static readonly Vector3i NorthBasis = new(1, 0, 0);
    public static readonly Vector3i SouthBasis = new(-1, 0, 0);
    public static readonly Vector3i EastBasis = new(0, 0, 1);
    public static readonly Vector3i WestBasis = new(0, 0, -1);

    /// <summary>
    /// Converts a world space position into the position of the block containing it. Rounding has to floor
    /// rather than truncate, otherwise every block in the range (-1, 0) would map onto block 0 alongside the
    /// blocks in (0, 1).
    /// </summary>
    public static Vector3i ToBlockPos(this Vector3 position)
    {
        return new Vector3i(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y),
            (int)MathF.Floor(position.Z));
    }

    public static Vector3 ToFloat(this Vector3i position)
    {
        return new Vector3(position.X, position.Y, position.Z);
    }

    /// <summary>
    /// Maps a world space block position onto its position within its chunk. Y is already chunk local since
    /// chunks span the full build height.
    /// </summary>
    public static Vector3i ToChunkLocal(this Vector3i position)
    {
        return new Vector3i(position.X & 15, position.Y, position.Z & 15);
    }

    public static Vector3i Up(this Vector3i position) => new(position.X, position.Y + 1, position.Z);

    public static Vector3i Down(this Vector3i position) => new(position.X, position.Y - 1, position.Z);

    public static Vector3i North(this Vector3i position) => position + NorthBasis;

    public static Vector3i South(this Vector3i position) => position + SouthBasis;

    public static Vector3i East(this Vector3i position) => position + EastBasis;

    public static Vector3i West(this Vector3i position) => position + WestBasis;

    public static Vector3i[] GetSurroundingPositions(this Vector3i position)
    {
        return
        [
            position.North(),
            position.South(),
            position.East(),
            position.West(),
            position.Up(),
            position.Down(),
        ];
    }

    public static Vector3i[] GetSurroundingPositionsBesidesUp(this Vector3i position)
    {
        return
        [
            position.North(),
            position.South(),
            position.East(),
            position.West(),
            position.Down(),
        ];
    }

    public static double Distance(this Vector3i from, Vector3i to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        int dz = to.Z - from.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static int ManhattanDistance(this Vector3i from, Vector3i to)
    {
        return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) + Math.Abs(to.Z - from.Z);
    }
}
