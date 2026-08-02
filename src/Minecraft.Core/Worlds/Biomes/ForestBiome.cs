using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class ForestBiome : Biome
{
    private const double TerrainDetail = 0.005D;
    private const double HeightVariation = 32;

    /// <summary>
    /// Every biome samples the one shared noise field, so each takes its own slice of the domain to keep
    /// their height maps from being copies of one another.
    /// </summary>
    private const float DomainOffset = 0F;

    protected override void DefineProperties()
    {
        BaseHeight = 0;
        Temperature = 0.1D;
        Moisture = 0.9D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        Decorator = new ForestDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int chunkX, int chunkZ, int localX, int localZ)
    {
        const double chunkDim = 16;
        double y = chunkX * chunkDim * TerrainDetail + localX * TerrainDetail;
        double x = chunkZ * chunkDim * TerrainDetail + localZ * TerrainDetail;
        return BaseHeight + Noise2DPerlin.Noise01((float)x + DomainOffset, (float)y + DomainOffset) * HeightVariation;
    }
}
