using Vector3i = Minecraft.Core.Utilities.Vector.Vector3i;

namespace Minecraft.Core.Utilities;

public static class DirectionUtil
{
    private static readonly Vector3i Back = new(0, 0, -1);
    private static readonly Vector3i Right = new(1, 0, 0);
    private static readonly Vector3i Front = new(0, 0, 1);
    private static readonly Vector3i Left = new(-1, 0, 0);
    private static readonly Vector3i Top = new(0, 1, 0);
    private static readonly Vector3i Bottom = new(0, -1, 0);

    public static Direction Invert(Direction direction)
    {
        return direction switch
        {
            Direction.Back => Direction.Front,
            Direction.Front => Direction.Back,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            Direction.Top => Direction.Bottom,
            Direction.Bottom => Direction.Top,
            _ => throw new NotImplementedException(),
        };
    }

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
            _ => throw new NotImplementedException(),
        };
    }
}