using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class SwampBiome : Biome
{
    private const float TerrainDetail = 0.0075F;
    private const double HeightVariation = 7;

    private const double SunkenBy = 2.6D;

    private const float HummockDetail = 0.055F;
    private const double HummockVariation = 1.6D;

    private const float DomainOffset = 8317.41F;

    protected override void DefineProperties()
    {
        BaseHeight = 0;
        Temperature = 0.62D;
        Moisture = 0.96D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Clay;

        HasShoreline = false;

        Decorator = new SwampDecorator();

        SettlementPalette = null;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;
        double ground = Noise2DPerlinOctave.Noise01(x, y, octaves: 2) * HeightVariation;

        float hummockX = worldZ * HummockDetail + DomainOffset;
        float hummockY = worldX * HummockDetail + DomainOffset;
        double hummocks = Noise2DPerlin.Noise(hummockX, hummockY) * HummockVariation;

        return BaseHeight + ground + hummocks - SunkenBy;
    }
}
