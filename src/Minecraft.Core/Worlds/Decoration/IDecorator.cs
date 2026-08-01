using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

public interface IDecorator
{
    /// <summary>
    /// Places whatever sits on top of the terrain surface in one column.
    /// </summary>
    /// <param name="random">
    /// Seeded from the world seed and the chunk position, so a chunk always decorates the same way. Do not
    /// substitute an unseeded source here: unmodified chunks are regenerated rather than stored on disk, and
    /// that only holds while generation is reproducible.
    /// </param>
    void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random);
}
