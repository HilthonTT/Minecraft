using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Works out the bare terrain of a world column: which biome it belongs to and how high its surface is,
/// before caves are carved or anything is grown on it.
/// <para>
/// It reads nothing but the seeded noise fields, so it answers for columns whose chunk does not exist. That
/// is what lets a structure spanning several chunks agree with itself about the ground it is standing on.
/// </para>
/// </summary>
public sealed class TerrainSampler : ITerrainSampler
{
    /// <summary>How quickly the climate varies across the world. Smaller means larger biomes.</summary>
    private const double TemperatureDetail = 0.0021D;
    private const double MoistureDetail = 0.0026D;

    /// <summary>
    /// How many octaves the climate is sampled over. Enough to give a biome border a ragged edge, but few
    /// enough that the borders stay long and sweeping rather than dissolving into speckle.
    /// </summary>
    private const int ClimateOctaves = 3;

    /// <summary>
    /// How hard the climate samples are spread out over their range. Perlin noise bunches up around zero, and
    /// read straight off it nearly the whole world would sit in the middle of climate space in a single
    /// biome. Chosen to match the spread of the field it flattens; see <see cref="TerrainNoise.Spread01"/>.
    /// </summary>
    private const float ClimateSoftness = 0.09F;

    /// <summary>
    /// Temperature and moisture come from the same shared noise field, so moisture is sampled from a distant
    /// part of the domain to keep the two climate axes independent.
    /// </summary>
    private const float MoistureDomainOffset = 2555.5F;

    private readonly BiomeProvider _biomeProvider;
    private readonly int _seaLevel;
    private readonly int _lowestSurface;

    /// <param name="lowestSurface">
    /// The lowest a surface may sit, which has to leave room for the layers the generator puts underneath it.
    /// </param>
    public TerrainSampler(Biome[] biomes, int seaLevel, int lowestSurface)
    {
        _biomeProvider = new BiomeProvider(biomes);
        _seaLevel = seaLevel;
        _lowestSurface = lowestSurface;
    }

    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        double temperature = TerrainNoise.Spread01(
            Noise2DPerlinOctave.Noise(
                (float)(worldZ * TemperatureDetail),
                (float)(worldX * TemperatureDetail),
                ClimateOctaves),
            ClimateSoftness);

        double moisture = TerrainNoise.Spread01(
            Noise2DPerlinOctave.Noise(
                (float)(worldZ * MoistureDetail) + MoistureDomainOffset,
                (float)(worldX * MoistureDetail) + MoistureDomainOffset,
                ClimateOctaves),
            ClimateSoftness);

        BiomeMembership[] memberships = _biomeProvider.GetBiomeMemberships(temperature, moisture);

        // The surface blocks come from whichever biome dominates, but the height is a blend of all of them so
        // that the transition between two biomes is a slope rather than a cliff.
        BiomeMembership dominant = memberships[0];
        double heightOffset = 0;
        foreach (BiomeMembership membership in memberships)
        {
            if (dominant.Percentage < membership.Percentage)
            {
                dominant = membership;
            }

            heightOffset += membership.Percentage * membership.Biome.OffsetAt(worldX, worldZ);
        }

        // Clamped so that neither the layers below the surface nor the decoration above it can ever fall
        // outside the build height.
        int surfaceY = Math.Clamp(
            _seaLevel + (int)heightOffset,
            _lowestSurface,
            Constants.MAX_BUILD_HEIGHT - 2);

        return new TerrainColumn(surfaceY, dominant.Biome);
    }
}
