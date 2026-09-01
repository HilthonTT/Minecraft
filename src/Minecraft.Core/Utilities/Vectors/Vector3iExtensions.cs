using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Vectors;

public static class Vector3iExtensions
{
    public static readonly Vector3i NorthBasis = new(1, 0, 0);
    public static readonly Vector3i SouthBasis = new(-1, 0, 0);
    public static readonly Vector3i EastBasis = new(0, 0, 1);
    public static readonly Vector3i WestBasis = new(0, 0, -1);

    public static Vector3i ToBlockPos(this Vector3 position)
    {
        return new Vector3i(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y),
            (int)MathF.Floor(position.Z));
    }

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
}
