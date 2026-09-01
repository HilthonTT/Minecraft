namespace Minecraft.Core.Worlds.Structures;

public interface ITerrainSampler
{
    TerrainColumn SampleColumn(int worldX, int worldZ);

    int SeaLevel { get; }
}
