using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class MountainBiome : Biome
{
    private const double TerrainDetail = 0.005D;
    private const double HeightVariation = 64;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 613.27F;

    protected override void DefineProperties()
    {
        BaseHeight = 8;
        Temperature = 0.9D;
        Moisture = 0.5D;
        TopBlock = BlockRegistry.Stone;
        GradientBlock = BlockRegistry.Stone;
        Decorator = new RockyDecorator();
    }

    public override double OffsetAt(int chunkX, int chunkZ, int localX, int localZ)
    {
        const double chunkDim = 16;
        double y = chunkX * chunkDim * TerrainDetail + localX * TerrainDetail;
        double x = chunkZ * chunkDim * TerrainDetail + localZ * TerrainDetail;
        return BaseHeight + Noise2DPerlin.Noise01((float)x + DomainOffset, (float)y + DomainOffset) * HeightVariation;
    }
}
