namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// Remembers the columns it has already sampled.
/// <para>
/// Laying out a structure walks over the same columns several times, once to judge how flat the ground is and
/// again to level it, and every sample costs a handful of noise lookups. One of these is made for each chunk
/// being generated and thrown away with it, so it never grows past the few thousand columns a structure
/// spans.
/// </para>
/// </summary>
public sealed class CachedTerrainSampler(ITerrainSampler source) : ITerrainSampler
{
    private readonly Dictionary<(int X, int Z), TerrainColumn> _columns = [];

    public int SeaLevel => source.SeaLevel;

    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        (int X, int Z) key = (worldX, worldZ);
        if (_columns.TryGetValue(key, out TerrainColumn column))
        {
            return column;
        }

        column = source.SampleColumn(worldX, worldZ);
        _columns.Add(key, column);
        return column;
    }
}
