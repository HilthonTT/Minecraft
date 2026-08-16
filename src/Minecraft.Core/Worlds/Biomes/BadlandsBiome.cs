using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Dry country worn down into flat topped mesas. Its height is stepped hard, so what stands between one
/// table of level ground and the next is a wall rather than a slope, and the ground is laid down in bands of
/// sand, sandstone and clay that the walls cut through.
/// <para>
/// The banding is by height rather than by depth, which is what makes a band run level all the way around a
/// mesa and carry on at the same height on the next one along, the way a bed of rock does.
/// </para>
/// </summary>
public sealed class BadlandsBiome : Biome
{
    private const float TerrainDetail = 0.0030F;
    private const double HeightVariation = 46;

    /// <summary>How many tables the height range is cut into, and how much of each one is level.</summary>
    private const int MesaCount = 6;
    private const float MesaFlatness = 0.86F;

    /// <summary>
    /// A tighter field added on top of the steps, so the walls between them are gullied and the tops are not
    /// perfectly true. Small enough that it breaks the edges up without rounding the steps off.
    /// </summary>
    private const float GullyDetail = 0.020F;
    private const double GullyVariation = 3.5D;

    /// <summary>
    /// The thickness of one band of rock, and the height the stack of them is counted from. Counted from a
    /// fixed height rather than from sea level so the bands line up across the whole biome.
    /// </summary>
    private const int BandHeight = 4;
    private const int BandOrigin = 60;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
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

        // Nothing is settled out here. A village wants level ground and there is little of it that is not
        // the top of a cliff.
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

        // Sand over the band it belongs to, so a table top reads as drifted over while its wall shows what
        // is underneath.
        return (band == BlockRegistry.SandStone ? BlockRegistry.Sand : band, band);
    }

    public override Block CliffAt(int surfaceY) => BandAt(surfaceY);

    /// <summary>
    /// The bed of rock that lies at a given height. Four beds repeating, two of them sandstone so that the
    /// clay and the stone read as seams running through it rather than as stripes of equal weight.
    /// </summary>
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
