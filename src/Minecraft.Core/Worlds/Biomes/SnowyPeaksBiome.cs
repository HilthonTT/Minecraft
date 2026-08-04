using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// The cold end of the world: pine covered slopes that climb past the snow line into bare white summits. It
/// reaches higher than any other biome, so it is what puts the tallest peaks in the world.
/// </summary>
public sealed class SnowyPeaksBiome : Biome
{
    private const float TerrainDetail = 0.0085F;
    private const double HeightVariation = 86;

    /// <summary>
    /// A broader field the ridges are multiplied by, so that some stretches of the range rear up into summits
    /// while others stay as shoulders below the snow line.
    /// </summary>
    private const float MassifDetail = 0.0011F;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 4903.61F;

    protected override void DefineProperties()
    {
        BaseHeight = 22;
        Temperature = 0.12D;
        Moisture = 0.30D;
        TopBlock = BlockRegistry.SnowyGrass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new SnowyDecorator();
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;
        double ridges = TerrainNoise.Ridged01(x, y, octaves: 5, persistence: 0.5F);

        float massifX = worldZ * MassifDetail + DomainOffset;
        float massifY = worldX * MassifDetail + DomainOffset;
        double massif = 0.4D + (0.6D * Noise2DPerlin.Noise01(massifX, massifY));

        return BaseHeight + (ridges * massif * HeightVariation);
    }
}
