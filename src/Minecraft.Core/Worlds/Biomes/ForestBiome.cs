using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Wooded, rolling country. Its height comes from several octaves rather than one, so the ground beneath the
/// trees has knolls and hollows in it instead of a single smooth swell.
/// </summary>
public sealed class ForestBiome : Biome
{
    private const float TerrainDetail = 0.005F;
    private const double HeightVariation = 22;

    /// <summary>
    /// Every biome samples the one shared noise field, so each takes its own slice of the domain to keep
    /// their height maps from being copies of one another.
    /// </summary>
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
