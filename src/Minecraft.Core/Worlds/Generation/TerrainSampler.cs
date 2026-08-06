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

    /// <summary>
    /// How quickly land gives way to sea. Much broader than the climate, because an ocean has to be wide
    /// enough that standing on one shore does not show the other.
    /// </summary>
    private const double ContinentDetail = 0.00055D;
    private const int ContinentOctaves = 3;
    private const float ContinentSoftness = 0.10F;
    private const float ContinentDomainOffset = 8821.9F;

    /// <summary>
    /// Where the sea ends and the land begins, on the continent field. Between the two the ground climbs out
    /// of the water gradually, which is what puts a shelf and then a beach between a sea and the land behind
    /// it rather than a wall.
    /// </summary>
    private const double OceanEdge = 0.30D;
    private const double CoastEdge = 0.46D;

    /// <summary>How far below sea level the floor of an ocean lies where it is furthest from any land.</summary>
    private const double OceanDepth = 30D;

    /// <summary>
    /// How much of that depth is actually reached, as a field of its own, so that a sea has deeps and
    /// shallows rather than one flat bottom at exactly the same height everywhere.
    /// </summary>
    private const double BasinShallowest = 0.45D;
    private const double OceanFloorDetail = 0.0032D;
    private const int OceanFloorOctaves = 3;
    private const float OceanFloorDomainOffset = 6203.4F;

    /// <summary>
    /// How much of a biome's own relief survives under water. Small, so the sea floor rolls gently rather
    /// than putting the hills of whatever biome nominally covers it under the waves, but not zero: at zero
    /// the whole ocean floor collapses onto a single height and reads as a tiled plane.
    /// </summary>
    private const double UnderwaterReliefShare = 0.18D;

    /// <summary>
    /// The river network. One low frequency field, carved along the line where it crosses zero: that line
    /// wanders and branches the way a river does, and it never ends abruptly, since a contour of a
    /// continuous field either closes on itself or runs forever.
    /// </summary>
    private const double RiverDetail = 0.0014D;
    private const int RiverOctaves = 2;
    private const float RiverDomainOffset = 4409.1F;

    /// <summary>How far either side of the zero line the channel reaches, in units of the field.</summary>
    private const float RiverWidth = 0.035F;

    /// <summary>How far below sea level a river bed is cut, so that a river holds water rather than damp.</summary>
    private const double RiverDepth = 4D;

    /// <summary>
    /// The deepest a river will cut into the ground it crosses. Without a limit a channel running over a
    /// mountain would trench the whole range down to sea level; with one it leaves a valley through the
    /// highlands instead, and only fills with water once the land it is crossing is low enough.
    /// </summary>
    private const double MaxRiverCut = 20D;

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

    public int SeaLevel => _seaLevel;

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

        // How much of this column is land rather than sea floor. The biome's own height is faded out along
        // with it, so a hill does not carry on out under the water: what is left at sea is the basin, and
        // between the two is a shore that climbs rather than a coastline that steps.
        double land = LandShareAt(worldX, worldZ);

        // A little of the biome's relief is kept under water so the floor still rolls. Faded out entirely
        // there would be nothing left but the basin, and the basin alone is the same height everywhere.
        double relief = land + ((1D - land) * UnderwaterReliefShare);
        double height = (heightOffset * relief) - (OceanDepth * BasinDepthAt(worldX, worldZ) * (1D - land));

        height = CarveRiverAt(worldX, worldZ, height);

        // Clamped so that neither the layers below the surface nor the decoration above it can ever fall
        // outside the build height.
        int surfaceY = Math.Clamp(
            _seaLevel + (int)height,
            _lowestSurface,
            Constants.MAX_BUILD_HEIGHT - 2);

        return new TerrainColumn(surfaceY, dominant.Biome);
    }

    /// <summary>
    /// How much of a column stands above the waterline as land, from zero out at sea to one well inland.
    /// </summary>
    private static double LandShareAt(int worldX, int worldZ)
    {
        double continent = TerrainNoise.Spread01(
            Noise2DPerlinOctave.Noise(
                (float)(worldZ * ContinentDetail) + ContinentDomainOffset,
                (float)(worldX * ContinentDetail) + ContinentDomainOffset,
                ContinentOctaves),
            ContinentSoftness);

        return Smoothstep(OceanEdge, CoastEdge, continent);
    }

    /// <summary>
    /// What share of the full ocean depth the basin reaches at a column, so that a sea has troughs and
    /// banks running through it instead of one flat floor at a single height.
    /// </summary>
    private static double BasinDepthAt(int worldX, int worldZ)
    {
        float shape = Noise2DPerlinOctave.Noise01(
            (float)(worldZ * OceanFloorDetail) + OceanFloorDomainOffset,
            (float)(worldX * OceanFloorDetail) + OceanFloorDomainOffset,
            OceanFloorOctaves);

        return BasinShallowest + ((1D - BasinShallowest) * shape);
    }

    /// <summary>
    /// Cuts a river channel through a column's height, where one runs across it.
    /// </summary>
    /// <returns>
    /// The height with the channel taken out of it. A river only ever lowers ground: where the bed it wants
    /// is already above the ground there is, the column is left as it was, which is what stops a channel
    /// crossing a sea from building a ridge along the bottom of it.
    /// </returns>
    private static double CarveRiverAt(int worldX, int worldZ, double height)
    {
        float channel = Noise2DPerlinOctave.Noise(
            (float)(worldZ * RiverDetail) + RiverDomainOffset,
            (float)(worldX * RiverDetail) + RiverDomainOffset,
            RiverOctaves);

        // One at the middle of the channel, falling to nothing at its banks.
        double strength = 1D - Smoothstep(0D, RiverWidth, Math.Abs(channel));
        if (strength <= 0D)
        {
            return height;
        }

        double bed = Math.Max(-RiverDepth, height - MaxRiverCut);
        if (bed >= height)
        {
            return height;
        }

        return height + ((bed - height) * strength);
    }

    /// <summary>A value eased from zero at <paramref name="edge0"/> to one at <paramref name="edge1"/>.</summary>
    private static double Smoothstep(double edge0, double edge1, double value)
    {
        double t = Math.Clamp((value - edge0) / (edge1 - edge0), 0D, 1D);
        return t * t * (3D - (2D * t));
    }
}
