using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class MountainBiome : Biome
{
    private const float TerrainDetail = 0.010F;
    private const double HeightVariation = 72;

    private const float MassifDetail = 0.0009F;

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

        double massif = 0.35D + (0.65D * Noise2DPerlin.Noise01(massifX, massifY));

        return BaseHeight + (ridges * massif * HeightVariation);
    }
}
