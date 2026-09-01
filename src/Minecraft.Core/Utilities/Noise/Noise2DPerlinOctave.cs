namespace Minecraft.Core.Utilities.Noise;

public static class Noise2DPerlinOctave
{
    public const int DefaultOctaves = 4;
    public const float DefaultPersistence = 0.5f;
    public const float DefaultLacunarity = 2f;
    public const float DefaultFrequency = 1f;

    private readonly static float[] _octaveOffsetsX = [0f, 71.13f, 149.71f, 233.29f, 311.87f, 397.41f, 479.03f, 563.61f];
    private readonly static float[] _octaveOffsetsY = [0f, 131.57f, 209.19f, 293.77f, 367.31f, 443.93f, 521.51f, 607.09f];

    public static float Noise(
        float x,
        float y,
        int octaves = DefaultOctaves,
        float persistence = DefaultPersistence,
        float lacunarity = DefaultLacunarity,
        float frequency = DefaultFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        var total = 0f;
        var amplitude = 1f;
        var maxAmplitude = 0f;

        for (var i = 0; i < octaves; i++)
        {
            var offsetX = _octaveOffsetsX[i % _octaveOffsetsX.Length];
            var offsetY = _octaveOffsetsY[i % _octaveOffsetsY.Length];

            total += Noise2DPerlin.Noise(x * frequency + offsetX, y * frequency + offsetY) * amplitude;
            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxAmplitude > 0f ? Math.Clamp(total / maxAmplitude, -1f, 1f) : 0f;
    }

    public static float Noise01(
        float x,
        float y,
        int octaves = DefaultOctaves,
        float persistence = DefaultPersistence,
        float lacunarity = DefaultLacunarity,
        float frequency = DefaultFrequency)
    {
        return (Noise(x, y, octaves, persistence, lacunarity, frequency) + 1f) * 0.5f;
    }
}
