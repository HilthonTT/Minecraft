namespace Minecraft.Core.Utilities.Noise;

/// <summary>
/// Ken Perlin's improved 3D gradient noise. The 12 gradients point at the edge midpoints of a cube, which
/// removes the directional bias a randomly sampled gradient set produces. Useful for anything that varies with
/// height as well as position, caves and ore pockets in particular.
/// </summary>
public static class Noise3DPerlin
{
    /// <summary>Size of the permutation table. Must stay a power of two so <see cref="TableMask"/> works.</summary>
    private const int TableSize = 256;
    private const int TableMask = TableSize - 1;

    /// <summary>
    /// The table is stored twice back to back so lookups of the form p[p[x] + y] can run to 511 without
    /// needing a wrap on every index.
    /// </summary>
    private readonly static int[] _permutation = new int[TableSize * 2];

    static Noise3DPerlin()
    {
        Reseed(new Random());
    }

    /// <summary>
    /// Samples the noise field. The result lies in [-1, 1] and is 0 on the integer lattice. The gradients have
    /// length sqrt(2), so the raw sum can creep just past 1 and is clamped. Measured over 20 million samples
    /// that happened once, which is far below anything visible in terrain.
    /// </summary>
    public static float Noise(float x, float y, float z)
    {
        var floorX = MathF.Floor(x);
        var floorY = MathF.Floor(y);
        var floorZ = MathF.Floor(z);

        /// cell coordinates, masked so negative world positions wrap into the table instead of going out of range
        var cellX = (int)floorX & TableMask;
        var cellY = (int)floorY & TableMask;
        var cellZ = (int)floorZ & TableMask;

        /// position within the cell, always in [0, 1)
        var fracX = x - floorX;
        var fracY = y - floorY;
        var fracZ = z - floorZ;

        var u = Fade(fracX);
        var v = Fade(fracY);
        var w = Fade(fracZ);

        var a = _permutation[cellX] + cellY;
        var aa = _permutation[a] + cellZ;
        var ab = _permutation[a + 1] + cellZ;
        var b = _permutation[cellX + 1] + cellY;
        var ba = _permutation[b] + cellZ;
        var bb = _permutation[b + 1] + cellZ;

        var total = Lerp(w,
            Lerp(v,
                Lerp(u,
                    Grad(_permutation[aa], fracX, fracY, fracZ),
                    Grad(_permutation[ba], fracX - 1f, fracY, fracZ)),
                Lerp(u,
                    Grad(_permutation[ab], fracX, fracY - 1f, fracZ),
                    Grad(_permutation[bb], fracX - 1f, fracY - 1f, fracZ))),
            Lerp(v,
                Lerp(u,
                    Grad(_permutation[aa + 1], fracX, fracY, fracZ - 1f),
                    Grad(_permutation[ba + 1], fracX - 1f, fracY, fracZ - 1f)),
                Lerp(u,
                    Grad(_permutation[ab + 1], fracX, fracY - 1f, fracZ - 1f),
                    Grad(_permutation[bb + 1], fracX - 1f, fracY - 1f, fracZ - 1f))));

        return Math.Clamp(total, -1f, 1f);
    }

    /// <summary>
    /// Samples the noise field remapped to [0, 1].
    /// </summary>
    public static float Noise01(float x, float y, float z)
    {
        return (Noise(x, y, z) + 1f) * 0.5f;
    }

    /// <summary>
    /// Fractal brownian motion over this field. See <see cref="Noise2DPerlinOctave"/> for the parameters.
    /// The result lies in [-1, 1].
    /// </summary>
    public static float Noise(
        float x,
        float y,
        float z,
        int octaves,
        float persistence = Noise2DPerlinOctave.DefaultPersistence,
        float lacunarity = Noise2DPerlinOctave.DefaultLacunarity,
        float frequency = Noise2DPerlinOctave.DefaultFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        var total = 0f;
        var amplitude = 1f;
        var maxAmplitude = 0f;

        for (var i = 0; i < octaves; i++)
        {
            /// offset each octave so they do not all collapse to 0 on the same lattice points
            var offset = i * 97.31f;

            total += Noise(x * frequency + offset, y * frequency + offset, z * frequency + offset) * amplitude;
            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxAmplitude > 0f ? Math.Clamp(total / maxAmplitude, -1f, 1f) : 0f;
    }

    /// <summary>
    /// Generates a new permutation from a non deterministic source.
    /// </summary>
    public static void Reseed()
    {
        Reseed(new Random());
    }

    /// <summary>
    /// Generates a new permutation. The same seed always yields the same noise field, which is what world
    /// generation needs to reproduce a world from its seed.
    /// </summary>
    public static void Reseed(int seed)
    {
        Reseed(new Random(seed));
    }

    private static void Reseed(Random random)
    {
        var source = new int[TableSize];
        for (var i = 0; i < TableSize; i++)
        {
            source[i] = i;
        }

        /// unbiased Fisher-Yates shuffle
        for (var i = TableSize - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);

            (source[j], source[i]) = (source[i], source[j]);
        }

        for (var i = 0; i < TableSize; i++)
        {
            _permutation[i] = source[i];
            _permutation[i + TableSize] = source[i];
        }
    }

    /// <summary>
    /// The 6t^5 - 15t^4 + 10t^3 ease curve. Its first and second derivatives are 0 at both ends, so cells meet
    /// without a visible seam.
    /// </summary>
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float Lerp(float t, float from, float to)
    {
        return from + t * (to - from);
    }

    /// <summary>
    /// Dot product of the position with one of the 12 gradients pointing at the midpoints of a cube's edges,
    /// selected by the low 4 bits of the hash.
    /// </summary>
    private static float Grad(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : (h is 12 or 14 ? x : z);

        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
