using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Bare rock thrown up into ridges. The height comes from ridged noise, which folds the smooth valleys of an
/// ordinary field into creases, so the range has spines and gullies running through it rather than a
/// collection of separate domes.
/// </summary>
public sealed class MountainBiome : Biome
{
    private const float TerrainDetail = 0.010F;
    private const double HeightVariation = 72;

    /// <summary>
    /// A second, much broader field the ridges are multiplied by. It decides which stretches of the range
    /// rear up and which stay as foothills, so the mountains do not all reach the same height.
    /// </summary>
    private const float MassifDetail = 0.0009F;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 613.27F;

    protected override void DefineProperties()
    {
        BaseHeight = 16;
        Temperature = 0.30D;
        Moisture = 0.75D;
        TopBlock = BlockRegistry.Stone;
        GradientBlock = BlockRegistry.Stone;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new RockyDecorator();
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;
        double ridges = TerrainNoise.Ridged01(x, y, octaves: 5, persistence: 0.45F);

        float massifX = worldZ * MassifDetail + DomainOffset;
        float massifY = worldX * MassifDetail + DomainOffset;

        // Never all the way to zero, so a stretch of low massif still reads as hill country rather than as a
        // hole cut out of the range.
        double massif = 0.35D + (0.65D * Noise2DPerlin.Noise01(massifX, massifY));

        return BaseHeight + (ridges * massif * HeightVariation);
    }
}
