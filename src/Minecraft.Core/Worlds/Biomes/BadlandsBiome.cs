using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class BadlandsBiome : Biome
{
    private const float TerrainDetail = 0.0030F;
    private const double HeightVariation = 46;

    private const int MesaCount = 6;
    private const float MesaFlatness = 0.86F;

    private const float GullyDetail = 0.020F;
    private const double GullyVariation = 3.5D;

    private const int BandHeight = 4;
    private const int BandOrigin = 60;

    private const float DomainOffset = 5147.09F;

    protected override void DefineProperties()
    {
        BaseHeight = 8;
        Temperature = 0.95D;
        Moisture = 0.32D;
        TopBlock = BlockRegistry.Sand;
        GradientBlock = BlockRegistry.SandStone;
        CliffBlock = BlockRegistry.SandStone;
        Decorator = new BarrenDecorator();

        SettlementPalette = null;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        float height = Noise2DPerlinOctave.Noise01(x, y, octaves: 3, persistence: 0.42F);
        double mesas = TerrainNoise.Terrace01(height, MesaCount, MesaFlatness) * HeightVariation;

        float gullyX = worldZ * GullyDetail + DomainOffset;
        float gullyY = worldX * GullyDetail + DomainOffset;
        double gullies = Noise2DPerlin.Noise(gullyX, gullyY) * GullyVariation;

        return BaseHeight + mesas + gullies;
    }

    public override (Block Top, Block Gradient) SurfaceAt(int surfaceY)
    {
        Block band = BandAt(surfaceY);

        return (band == BlockRegistry.SandStone ? BlockRegistry.Sand : band, band);
    }

    public override Block CliffAt(int surfaceY) => BandAt(surfaceY);

    private static Block BandAt(int surfaceY)
    {
        int band = Math.Abs(surfaceY - BandOrigin) / BandHeight;

        return (band % 4) switch
        {
            0 => BlockRegistry.SandStone,
            1 => BlockRegistry.Clay,
            2 => BlockRegistry.SandStone,
            _ => BlockRegistry.Stone,
        };
    }
}
