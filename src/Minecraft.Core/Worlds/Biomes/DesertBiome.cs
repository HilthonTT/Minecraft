using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public sealed class DesertBiome : Biome
{
    private const double TerrainDetail = 0.0005D;
    private const double HeightVariation = 16;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 1289.51F;

    protected override void DefineProperties()
    {
        BaseHeight = 0;
        Temperature = 0.5D;
        Moisture = 0.1D;
        TopBlock = BlockRegistry.Sand;
        GradientBlock = BlockRegistry.SandStone;
        Decorator = new BarrenDecorator();
        SettlementPalette = StructurePalette.Sandstone;
    }

    public override double OffsetAt(int chunkX, int chunkZ, int localX, int localZ)
    {
        const double chunkDim = 16;
        double y = chunkX * chunkDim * TerrainDetail + localX * TerrainDetail;
        double x = chunkZ * chunkDim * TerrainDetail + localZ * TerrainDetail;
        return BaseHeight + Noise2DPerlinOctave.Noise01((float)x + DomainOffset, (float)y + DomainOffset) * HeightVariation;
    }
}
