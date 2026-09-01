namespace Minecraft.Core.Worlds.Structures;

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
