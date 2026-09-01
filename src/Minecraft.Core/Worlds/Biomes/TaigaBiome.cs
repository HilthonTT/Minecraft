using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class TaigaBiome : Biome
{
    private const float TerrainDetail = 0.0055F;
    private const double HeightVariation = 16;

    private const float DomainOffset = 6421.83F;

    protected override void DefineProperties()
    {
        BaseHeight = 4;
        Temperature = 0.22D;
        Moisture = 0.62D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new TaigaDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        return BaseHeight + Noise2DPerlinOctave.Noise01(x, y, octaves: 3, persistence: 0.42F) * HeightVariation;
    }
}
