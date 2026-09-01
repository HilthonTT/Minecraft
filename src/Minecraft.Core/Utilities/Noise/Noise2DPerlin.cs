using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Noise;

public static class Noise2DPerlin
{
    private const int TableSize = 256;
    private const int TableMask = TableSize - 1;

    private readonly static int[] _permutation = new int[TableSize];
    private readonly static Vector2[] _gradients = new Vector2[TableSize];

    private readonly static Vector2[] _corners =
    [
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 0),
        new Vector2(1, 1)
    ];

    static Noise2DPerlin()
    {
        Reseed(new Random());
    }

    public static float Noise(float x, float y)
    {
        var cell = new Vector2(MathF.Floor(x), MathF.Floor(y));

        var total = 0f;

        foreach (var n in _corners)
        {
            var ij = cell + n;
            var uv = new Vector2(x - ij.X, y - ij.Y);

            var index = _permutation[(int)ij.X & TableMask];
            index = _permutation[(index + (int)ij.Y) & TableMask];

            var grad = _gradients[index & TableMask];

            total += Q(uv.X, uv.Y) * Vector2.Dot(grad, uv);
        }

        return Math.Clamp(total, -1f, 1f);
    }

    public static float Noise01(float x, float y)
    {
        return (Noise(x, y) + 1f) * 0.5f;
    }

    public static void Reseed()
    {
        Reseed(new Random());
    }

    public static void Reseed(int seed)
    {
        Reseed(new Random(seed));
    }

    private static void Reseed(Random random)
    {
        CalculatePermutation(_permutation, random);
        CalculateGradients(_gradients, random);
    }

    private static void CalculatePermutation(int[] p, Random random)
    {
        for (var i = 0; i < p.Length; i++)
        {
            p[i] = i;
        }

        for (var i = p.Length - 1; i > 0; i--)
        {
            var source = random.Next(i + 1);

            (p[source], p[i]) = (p[i], p[source]);
        }
    }

    private static void CalculateGradients(Vector2[] grad, Random random)
    {
        for (var i = 0; i < grad.Length; i++)
        {
            Vector2 gradient;

            do
            {
                gradient = new Vector2((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1));
            }
            while (gradient.LengthSquared >= 1 || gradient.LengthSquared == 0);

            gradient.Normalize();

            grad[i] = gradient;
        }
    }

    private static float Drop(float t)
    {
        t = Math.Abs(t);
        return 1f - t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static float Q(float u, float v)
    {
        return Drop(u) * Drop(v);
    }
}
