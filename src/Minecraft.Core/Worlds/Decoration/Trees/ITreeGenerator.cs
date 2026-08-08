using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public interface ITreeGenerator
{
    /// <summary>
    /// Grows a tree with its trunk at the given chunk local column.
    /// </summary>
    /// <param name="random">Seeded per chunk so that the same chunk always grows the same trees.</param>
    void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ, Random random);
}
