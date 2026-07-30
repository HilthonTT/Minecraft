namespace Minecraft.Core.Utilities.Noise;

/// <summary>
/// Fractal brownian motion layered on top of <see cref="Noise2DPerlin"/>. Each successive octave samples the
/// underlying field at a higher frequency and contributes less to the total, which turns the smooth single
/// octave field into something usable as a terrain height map.
/// <para>
/// The field itself is owned by <see cref="Noise2DPerlin"/>, so reseed through <see cref="Noise2DPerlin.Reseed(int)"/>.
/// </para>
/// </summary>
public static class Noise2DPerlinOctave
{
    public const int DefaultOctaves = 4;
    public const float DefaultPersistence = 0.5f;
    public const float DefaultLacunarity = 2f;
    public const float DefaultFrequency = 1f;

    /// <summary>
    /// Perlin noise is exactly 0 on the integer lattice, so with a lacunarity of 2 every octave would hit zero
    /// on the same points and leave a visible grid of pinch marks in the terrain. Shifting each octave by an
    /// arbitrary non integer amount decorrelates them.
    /// </summary>
    private readonly static float[] _octaveOffsetsX = [0f, 71.13f, 149.71f, 233.29f, 311.87f, 397.41f, 479.03f, 563.61f];
    private readonly static float[] _octaveOffsetsY = [0f, 131.57f, 209.19f, 293.77f, 367.31f, 443.93f, 521.51f, 607.09f];

    /// <summary>
    /// Samples the layered noise field. The result lies in [-1, 1].
    /// </summary>
    /// <param name="octaves">Number of noise layers to sum. Must be at least 1.</param>
    /// <param name="persistence">Factor applied to the amplitude of each successive octave. Below 1 for smooth terrain.</param>
    /// <param name="lacunarity">Factor applied to the frequency of each successive octave. Above 1 to add finer detail.</param>
    /// <param name="frequency">Frequency of the first octave. Higher values compress the features.</param>
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

        /// dividing by the summed amplitudes keeps the result inside the range of a single octave
        return maxAmplitude > 0f ? Math.Clamp(total / maxAmplitude, -1f, 1f) : 0f;
    }

    /// <summary>
    /// Samples the layered noise field remapped to [0, 1], the form usually wanted for height maps.
    /// </summary>
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
