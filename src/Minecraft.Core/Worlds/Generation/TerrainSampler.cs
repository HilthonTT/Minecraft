using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Generation;

public sealed class TerrainSampler : ITerrainSampler
{
    private const double TemperatureDetail = 0.0021D;
    private const double MoistureDetail = 0.0026D;

    private const int ClimateOctaves = 3;

    private const float ClimateSoftness = 0.09F;

    private const float MoistureDomainOffset = 2555.5F;

    private const double WarpDetail = 0.0013D;
    private const double WarpStrength = 120D;
    private const int WarpOctaves = 2;
    private const float WarpDomainOffsetX = 1601.3F;
    private const float WarpDomainOffsetZ = 9403.7F;

    private const double RiverWarpDetail = 0.0042D;
    private const double RiverWarpStrength = 34D;
    private const float RiverWarpDomainOffsetX = 3307.7F;
    private const float RiverWarpDomainOffsetZ = 6113.1F;

    private const double ContinentDetail = 0.00055D;
    private const int ContinentOctaves = 3;
    private const float ContinentSoftness = 0.10F;
    private const float ContinentDomainOffset = 8821.9F;

    private const double OceanEdge = 0.30D;
    private const double CoastEdge = 0.46D;

    private const double OceanDepth = 30D;

    private const double BasinShallowest = 0.45D;
    private const double OceanFloorDetail = 0.0032D;
    private const int OceanFloorOctaves = 3;
    private const float OceanFloorDomainOffset = 6203.4F;

    private const double UnderwaterReliefShare = 0.18D;

    private const double RiverDetail = 0.0014D;
    private const int RiverOctaves = 2;
    private const float RiverDomainOffset = 4409.1F;

    private const float RiverWidth = 0.035F;

    private const double RiverDepth = 4D;

    private const double MaxRiverCut = 20D;

    private readonly BiomeProvider _biomeProvider;
    private readonly int _seaLevel;
    private readonly int _lowestSurface;

    public TerrainSampler(Biome[] biomes, int seaLevel, int lowestSurface)
    {
        _biomeProvider = new BiomeProvider(biomes);
        _seaLevel = seaLevel;
        _lowestSurface = lowestSurface;
    }

    public int SeaLevel => _seaLevel;

    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        (double climateX, double climateZ) = Warp(
            worldX,
            worldZ,
            WarpDetail,
            WarpStrength,
            WarpOctaves,
            WarpDomainOffsetX,
            WarpDomainOffsetZ);

        double temperature = TerrainNoise.Spread01(
            Noise2DPerlinOctave.Noise(
                (float)(climateZ * TemperatureDetail),
                (float)(climateX * TemperatureDetail),
                ClimateOctaves),
            ClimateSoftness);

        double moisture = TerrainNoise.Spread01(
            Noise2DPerlinOctave.Noise(
                (float)(climateZ * MoistureDetail) + MoistureDomainOffset,
                (float)(climateX * MoistureDetail) + MoistureDomainOffset,
                ClimateOctaves),
            ClimateSoftness);

        BiomeMembership[] memberships = _biomeProvider.GetBiomeMemberships(temperature, moisture);

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

        double land = LandShareAt(worldX, worldZ);

        double relief = land + ((1D - land) * UnderwaterReliefShare);
        double height = (heightOffset * relief) - (OceanDepth * BasinDepthAt(worldX, worldZ) * (1D - land));

        height = CarveRiverAt(worldX, worldZ, height);

        int surfaceY = Math.Clamp(
            _seaLevel + (int)height,
            _lowestSurface,
            Constants.MAX_BUILD_HEIGHT - 2);

        return new TerrainColumn(surfaceY, dominant.Biome);
    }

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

    private static double BasinDepthAt(int worldX, int worldZ)
    {
        float shape = Noise2DPerlinOctave.Noise01(
            (float)(worldZ * OceanFloorDetail) + OceanFloorDomainOffset,
            (float)(worldX * OceanFloorDetail) + OceanFloorDomainOffset,
            OceanFloorOctaves);

        return BasinShallowest + ((1D - BasinShallowest) * shape);
    }

    private static double CarveRiverAt(int worldX, int worldZ, double height)
    {
        (double riverX, double riverZ) = Warp(
            worldX,
            worldZ,
            RiverWarpDetail,
            RiverWarpStrength,
            WarpOctaves,
            RiverWarpDomainOffsetX,
            RiverWarpDomainOffsetZ);

        float channel = Noise2DPerlinOctave.Noise(
            (float)(riverZ * RiverDetail) + RiverDomainOffset,
            (float)(riverX * RiverDetail) + RiverDomainOffset,
            RiverOctaves);

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

    private static (double X, double Z) Warp(
        int worldX,
        int worldZ,
        double detail,
        double strength,
        int octaves,
        float domainOffsetX,
        float domainOffsetZ)
    {
        float offsetX = Noise2DPerlinOctave.Noise(
            (float)(worldZ * detail) + domainOffsetX,
            (float)(worldX * detail) + domainOffsetX,
            octaves);

        float offsetZ = Noise2DPerlinOctave.Noise(
            (float)(worldZ * detail) + domainOffsetZ,
            (float)(worldX * detail) + domainOffsetZ,
            octaves);

        return (worldX + (offsetX * strength), worldZ + (offsetZ * strength));
    }

    private static double Smoothstep(double edge0, double edge1, double value)
    {
        double t = Math.Clamp((value - edge0) / (edge1 - edge0), 0D, 1D);
        return t * t * (3D - (2D * t));
    }
}
