using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities.Noise;

public static class Noise2DPerlin
{
    /// <summary>Size of the permutation and gradient tables. Must stay a power of two so <see cref="TableMask"/> works.</summary>
    private const int TableSize = 256;
    private const int TableMask = TableSize - 1;

    private readonly static int[] _permutation = new int[TableSize];
    private readonly static Vector2[] _gradients = new Vector2[TableSize];

    /// <summary>The four corners of the unit cell. Static so the hot path does not allocate per sample.</summary>
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

    /// <summary>
    /// Samples the noise field. The result lies in [-1, 1] and is 0 on the integer lattice.
    /// </summary>
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

    /// <summary>
    /// Generates a new permutation and gradient set from a non deterministic source.
    /// </summary>
    public static void Reseed()
    {
        Reseed(new Random());
    }

    /// <summary>
    /// Generates a new permutation and gradient set. The same seed always yields the same noise field,
    /// which is what world generation needs to reproduce a world from its seed.
    /// </summary>
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

        /// unbiased Fisher-Yates shuffle
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
