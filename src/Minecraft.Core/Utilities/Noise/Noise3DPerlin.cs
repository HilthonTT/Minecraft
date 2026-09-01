namespace Minecraft.Core.Utilities.Noise;

public static class Noise3DPerlin
{
    private const int TableSize = 256;
    private const int TableMask = TableSize - 1;

    private readonly static int[] _permutation = new int[TableSize * 2];

    static Noise3DPerlin()
    {
        Reseed(new Random());
    }

    public static float Noise(float x, float y, float z)
    {
        var floorX = MathF.Floor(x);
        var floorY = MathF.Floor(y);
        var floorZ = MathF.Floor(z);

        var cellX = (int)floorX & TableMask;
        var cellY = (int)floorY & TableMask;
        var cellZ = (int)floorZ & TableMask;

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

    public static float Noise01(float x, float y, float z)
    {
        return (Noise(x, y, z) + 1f) * 0.5f;
    }

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
            var offset = i * 97.31f;

            total += Noise(x * frequency + offset, y * frequency + offset, z * frequency + offset) * amplitude;
            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxAmplitude > 0f ? Math.Clamp(total / maxAmplitude, -1f, 1f) : 0f;
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
        var source = new int[TableSize];
        for (var i = 0; i < TableSize; i++)
        {
            source[i] = i;
        }

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

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float Lerp(float t, float from, float to)
    {
        return from + t * (to - from);
    }

    private static float Grad(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : (h is 12 or 14 ? x : z);

        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
