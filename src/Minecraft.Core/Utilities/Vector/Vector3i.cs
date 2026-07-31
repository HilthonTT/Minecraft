using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Vector;

public struct Vector3i : IEquatable<Vector3i>
{
    public readonly static Vector3i NorthBasis = new(1, 0, 0);
    public readonly static Vector3i SouthBasis = new(-1, 0, 0);
    public readonly static Vector3i EastBasis = new(0, 0, 1);
    public readonly static Vector3i WestBasis = new(0, 0, -1);
    public readonly static Vector3i Zero = new(0, 0, 0);
    public readonly static Vector3i One = new(1, 1, 1);

    public int X;
    public int Y;
    public int Z;

    public Vector3i(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public Vector3i(Vector3 vector3f, bool snapToGrid = true)
    {
        if (snapToGrid)
        {
            X = (int)MathF.Floor(vector3f.X);
            Y = (int)MathF.Floor(vector3f.Y);
            Z = (int)MathF.Floor(vector3f.Z);
        }
        else
        {
            X = (int)vector3f.X;
            Y = (int)vector3f.Y;
            Z = (int)vector3f.Z;
        }
    }

    public readonly Vector3 ToFloat()
    {
        return new Vector3(X, Y, Z);
    }

    public readonly Vector3i ToChunkLocal()
    {
        return new Vector3i(X & 15, Y, Z & 15);
    }

    public static Vector3i operator +(Vector3i a, Vector3i b)
    {
        return new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3i operator -(Vector3i a, Vector3i b)
    {
        return new Vector3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static bool operator ==(Vector3i a, Vector3i b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    public static bool operator !=(Vector3i a, Vector3i b)
    {
        return !(a == b);
    }

    public override readonly string ToString()
    {
        return "Vector3i(" + X + "," + Y + "," + Z + ")";
    }

    public readonly Vector3i Up()
    {
        return new Vector3i(X, Y + 1, Z);
    }

    public readonly Vector3i Down()
    {
        return new Vector3i(X, Y - 1, Z);
    }

    public readonly Vector3i East()
    {
        return this + EastBasis;
    }

    public readonly Vector3i West()
    {
        return this + WestBasis;
    }

    public readonly Vector3i North()
    {
        return this + NorthBasis;
    }

    public readonly Vector3i South()
    {
        return this + SouthBasis;
    }

    public readonly Vector3i[] GetSurroundingPositions()
    {
        return [North(), South(), East(), West(), Up(), Down()];
    }

    public readonly Vector3i[] GetSurroundingPositionsBesidesUp()
    {
        return [North(), South(), East(), West(), Down()];
    }

    public readonly double Distance(Vector3i vec)
    {
        return Math.Sqrt((vec.X - X) * (vec.X - X) + (vec.Y - Y) * (vec.Y - Y) + (vec.Z - Z) * (vec.Z - Z));
    }

    public readonly int ManhattanDistance(Vector3i vec)
    {
        return Math.Abs(vec.X - X) + Math.Abs(vec.Y - Y) + Math.Abs(vec.Z - Z);
    }

    public readonly bool Equals(Vector3i other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Vector3i other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}
