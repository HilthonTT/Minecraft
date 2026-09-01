namespace Minecraft.Core.Utilities.Noise;

public static class TerrainNoise
{
    private readonly static float[] _octaveOffsets = [0F, 83.17F, 167.41F, 251.93F, 337.61F, 421.29F, 509.83F, 593.47F];

    public static float Ridged01(
        float x,
        float y,
        int octaves = 4,
        float persistence = 0.5F,
        float lacunarity = 2F)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        float total = 0F;
        float amplitude = 1F;
        float maxAmplitude = 0F;
        float frequency = 1F;

        for (int i = 0; i < octaves; i++)
        {
            float offset = _octaveOffsets[i % _octaveOffsets.Length];

            float ridge = 1F - MathF.Abs(Noise2DPerlin.Noise(x * frequency + offset, y * frequency + offset));
            total += ridge * ridge * amplitude;
            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return Math.Clamp(total / maxAmplitude, 0F, 1F);
    }

    public static float Terrace01(float value, int steps, float flatness)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);

        float scaled = Math.Clamp(value, 0F, 1F) * steps;
        float step = MathF.Floor(scaled);
        float withinStep = scaled - step;

        float rise = flatness >= 1F ? 0F : Math.Clamp(withinStep / (1F - flatness), 0F, 1F);

        return (step + rise) / steps;
    }

    public static float Spread01(float signedNoise, float softness)
    {
        return 1F / (1F + MathF.Exp(-signedNoise / softness));
    }
}
