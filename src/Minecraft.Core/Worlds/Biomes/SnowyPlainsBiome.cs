using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class SnowyPlainsBiome : Biome
{
    private const float TerrainDetail = 0.0048F;
    private const double HeightVariation = 8;

    private const float DomainOffset = 7229.13F;

    protected override void DefineProperties()
    {
        BaseHeight = 2;
        Temperature = 0.10D;
        Moisture = 0.70D;
        TopBlock = BlockRegistry.SnowyGrass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new SnowyPlainsDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        return BaseHeight + Noise2DPerlinOctave.Noise01(x, y, octaves: 2) * HeightVariation;
    }
}
