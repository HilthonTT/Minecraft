using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// Wet lowland that lies about the waterline rather than above it. Its height barely moves, and what movement
/// there is straddles sea level, so every hollow it makes is filled by the pass that puts water in a basin
/// and what comes out is a country of pools with narrow ground between them.
/// <para>
/// Nothing is placed here to make that happen: a swamp is the ordinary sea, met from a hair above instead of
/// well above.
/// </para>
/// </summary>
public sealed class SwampBiome : Biome
{
    /// <summary>The broad rise and fall of the ground, which is what decides where the water stands.</summary>
    private const float TerrainDetail = 0.0075F;
    private const double HeightVariation = 7;

    /// <summary>
    /// How far below the waterline the middle of that range sits. Set so a little over half of the biome
    /// ends up dry: much lower and it is a sea, much higher and it is a meadow.
    /// </summary>
    private const double SunkenBy = 2.6D;

    /// <summary>A tighter field laid over it, which breaks the banks up so a pool has a ragged edge.</summary>
    private const float HummockDetail = 0.055F;
    private const double HummockVariation = 1.6D;

    /// <inheritdoc cref="ForestBiome" path="/summary"/>
    private const float DomainOffset = 8317.41F;

    protected override void DefineProperties()
    {
        BaseHeight = 0;
        Temperature = 0.62D;
        Moisture = 0.96D;
        TopBlock = BlockRegistry.Grass;
        GradientBlock = BlockRegistry.Dirt;
        CliffBlock = BlockRegistry.Clay;

        // The water here is the swamp rather than a sea it happens to touch, so its banks keep their own
        // ground instead of being washed down to the sand a shore would leave.
        HasShoreline = false;

        Decorator = new SwampDecorator();

        // No villages: there is nowhere here that stays dry enough to put one, and a settlement standing in
        // a marsh would be built on stilts of its own foundation blocks.
        SettlementPalette = null;
    }

    public override double OffsetAt(int worldX, int worldZ)
    {
        float x = worldZ * TerrainDetail + DomainOffset;
        float y = worldX * TerrainDetail + DomainOffset;
        double ground = Noise2DPerlinOctave.Noise01(x, y, octaves: 2) * HeightVariation;

        float hummockX = worldZ * HummockDetail + DomainOffset;
        float hummockY = worldX * HummockDetail + DomainOffset;
        double hummocks = Noise2DPerlin.Noise(hummockX, hummockY) * HummockVariation;

        return BaseHeight + ground + hummocks - SunkenBy;
    }
}
