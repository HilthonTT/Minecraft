using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Sand as far as it goes. Its height is the sum of a broad swell and a much tighter ripple laid over it, so
/// the ground reads as dunes marching across a wide basin rather than as one smooth bowl.
/// </summary>
public sealed class DesertBiome : Biome
{
    /// <summary>The slow rise and fall of the basin the dunes sit in.</summary>
    private const float BasinDetail = 0.0016F;
    private const double BasinVariation = 10;

    /// <summary>The dunes themselves, an order of magnitude tighter than the basin.</summary>
    private const float DuneDetail = 0.022F;
    private const double DuneVariation = 6;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 1289.51F;

    protected override void DefineProperties()
    {
        BaseHeight = 0;
        Temperature = 0.88D;
        Moisture = 0.12D;
        TopBlock = BlockRegistry.Sand;
        GradientBlock = BlockRegistry.SandStone;
        CliffBlock = BlockRegistry.SandStone;
        Decorator = new BarrenDecorator();
        SettlementPalette = StructurePalette.Sandstone;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float basinX = worldZ * BasinDetail + DomainOffset;
        float basinY = worldX * BasinDetail + DomainOffset;
        double basin = Noise2DPerlinOctave.Noise01(basinX, basinY, octaves: 2) * BasinVariation;

        // Ridged rather than plain noise, so a dune has a crest and a hollow instead of being a smooth swell.
        float duneX = worldZ * DuneDetail + DomainOffset;
        float duneY = worldX * DuneDetail + DomainOffset;
        double dunes = TerrainNoise.Ridged01(duneX, duneY, octaves: 2, persistence: 0.4F) * DuneVariation;

        return BaseHeight + basin + dunes;
    }
}
