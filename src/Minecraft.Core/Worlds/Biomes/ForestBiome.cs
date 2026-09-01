using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class ForestBiome : Biome
{
    private const float TerrainDetail = 0.005F;
    private const double HeightVariation = 22;

    private const float DomainOffset = 0F;

    protected override void DefineProperties()
    {
        BaseHeight = 4;
        Temperature = 0.55D;
        Moisture = 0.85D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new ForestDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        return BaseHeight + Noise2DPerlinOctave.Noise01(x, y, octaves: 4, persistence: 0.45F) * HeightVariation;
    }
}
