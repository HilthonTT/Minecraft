namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// Answers what the terrain looks like at any world column, whether or not the chunk holding it exists.
/// <para>
/// A structure is larger than a chunk, so the chunk being generated only ever holds a slice of it. Levelling
/// a plot or laying a path still has to know how high the ground is across the whole structure, including the
/// parts that fall in neighbouring chunks. Those neighbours are not loaded, and waiting for them would
/// deadlock, so the ground is recomputed from the seed instead of read out of the world.
/// </para>
/// </summary>
public interface ITerrainSampler
{
    /// <summary>
    /// The terrain of one world column. Depends on nothing but the world seed and the position, so every
    /// chunk of a structure agrees on the ground it is being built on.
    /// </summary>
    TerrainColumn SampleColumn(int worldX, int worldZ);

    /// <summary>
    /// The height standing water fills up to. A column whose surface is at or below it is under water, which
    /// is no place to build.
    /// </summary>
    int SeaLevel { get; }
}
