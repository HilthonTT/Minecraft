using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class SavannaBiome : Biome
{
    private const float TerrainDetail = 0.0035F;
    private const double HeightVariation = 30;

    private const int PlateauCount = 5;
    private const float PlateauFlatness = 0.72F;

    private const float DomainOffset = 3571.19F;

    protected override void DefineProperties()
    {
        BaseHeight = 6;
        Temperature = 0.80D;
        Moisture = 0.55D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new SavannaDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        float height = Noise2DPerlinOctave.Noise01(x, y, octaves: 3, persistence: 0.4F);

        return BaseHeight + TerrainNoise.Terrace01(height, PlateauCount, PlateauFlatness) * HeightVariation;
    }
}
