using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Open grassland: the calmest terrain in the world, and the one biome that reads as somewhere to walk rather
/// than somewhere to climb. It sits in the middle of climate space, so it is also what neighbouring biomes
/// settle down into at their edges.
/// </summary>
public sealed class PlainsBiome : Biome
{
    private const float TerrainDetail = 0.006F;
    private const double HeightVariation = 9;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 2113.77F;

    protected override void DefineProperties()
    {
        BaseHeight = 2;
        Temperature = 0.45D;
        Moisture = 0.45D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Stone;
        Decorator = new PlainsDecorator();
        SettlementPalette = StructurePalette.Oak;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;

        // Two octaves only. More would put detail into ground that is meant to read as flat.
        return BaseHeight + Noise2DPerlinOctave.Noise01(x, y, octaves: 2) * HeightVariation;
    }
}
