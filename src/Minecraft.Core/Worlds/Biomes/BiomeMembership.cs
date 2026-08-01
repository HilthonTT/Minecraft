namespace Minecraft.Core.Worlds.Biomes;

/// <summary>
/// How strongly a single point belongs to one biome. Terrain blends between biomes rather than switching
/// at a hard border, so a point is a weighted mix of every registered biome.
/// </summary>
public struct BiomeMembership
{
    public double Percentage;
    public Biome Biome;
}
