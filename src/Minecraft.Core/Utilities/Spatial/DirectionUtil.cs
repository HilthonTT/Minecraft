using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Spatial;

public static class DirectionUtil
{
    private static readonly Vector3i Back = new(0, 0, -1);
    private static readonly Vector3i Right = new(1, 0, 0);
    private static readonly Vector3i Front = new(0, 0, 1);
    private static readonly Vector3i Left = new(-1, 0, 0);
    private static readonly Vector3i Top = new(0, 1, 0);
    private static readonly Vector3i Bottom = new(0, -1, 0);

    public static Vector3i ToUnit(Direction direction)
    {
        return direction switch
        {
            Direction.Back => Back,
            Direction.Front => Front,
            Direction.Left => Left,
            Direction.Right => Right,
            Direction.Top => Top,
            Direction.Bottom => Bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }
}
