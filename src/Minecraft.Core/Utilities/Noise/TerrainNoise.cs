namespace Minecraft.Core.Utilities.Noise;

/// <summary>
/// Shapes taken from the plain noise fields that are useful for terrain but are not noise in themselves:
/// ridges, terraces and a way of widening a distribution.
/// <para>
/// The underlying field is owned by <see cref="Noise2DPerlin"/>, so reseed through
/// <see cref="Noise2DPerlin.Reseed(int)"/>.
/// </para>
/// </summary>
public static class TerrainNoise
{
    /// <summary>
    /// Shifts each octave so they do not all fall to zero on the same lattice points, which would otherwise
    /// leave a regular grid of creases across the terrain.
    /// </summary>
    private readonly static float[] _octaveOffsets = [0F, 83.17F, 167.41F, 251.93F, 337.61F, 421.29F, 509.83F, 593.47F];

    /// <summary>
    /// Ridged noise, in [0, 1]. Folding the field about zero turns the smooth valleys of ordinary noise into
    /// creases, and squaring what is left pulls the low ground flat and leaves the ridges standing, which is
    /// what makes a mountain range read as ridges and valleys rather than as lumps.
    /// </summary>
    /// <param name="octaves">Number of layers to sum. Must be at least 1.</param>
    /// <param name="persistence">Factor on the amplitude of each successive octave.</param>
    /// <param name="lacunarity">Factor on the frequency of each successive octave.</param>
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

    /// <summary>
    /// Rounds a [0, 1] value onto a ladder of flat steps, keeping a slope between one step and the next. Used
    /// where terrain should read as plateaus with an escarpment between them rather than as a smooth hill.
    /// </summary>
    /// <param name="steps">How many plateaus the range is cut into.</param>
    /// <param name="flatness">
    /// The share of each step that is level, between 0 and 1. At 0 nothing changes; approaching 1 the steps
    /// become flat with a cliff between them.
    /// </param>
    public static float Terrace01(float value, int steps, float flatness)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);

        float scaled = Math.Clamp(value, 0F, 1F) * steps;
        float step = MathF.Floor(scaled);
        float withinStep = scaled - step;

        // The climb is squeezed into the part of the step that is not level, and the rest is held flat.
        float rise = flatness >= 1F ? 0F : Math.Clamp(withinStep / (1F - flatness), 0F, 1F);

        return (step + rise) / steps;
    }

    /// <summary>
    /// Spreads a noise sample evenly over [0, 1].
    /// <para>
    /// Perlin noise is bell shaped and bunches hard around zero: nine samples in ten of a four octave field
    /// land within a fifth of the middle of its range. A climate map read straight off it would put nearly
    /// the whole world into one biome and leave the rest as curiosities. Running the sample through an S
    /// curve of about the shape of its own cumulative distribution flattens that hump out, so every part of
    /// the range comes up about as often as every other and each biome gets its share of the world.
    /// </para>
    /// </summary>
    /// <param name="signedNoise">A sample in [-1, 1].</param>
    /// <param name="softness">
    /// Roughly the standard deviation of the field being flattened. Too small and the result collapses to the
    /// two ends of the range; too large and the hump in the middle survives.
    /// </param>
    public static float Spread01(float signedNoise, float softness)
    {
        return 1F / (1F + MathF.Exp(-signedNoise / softness));
    }
}
